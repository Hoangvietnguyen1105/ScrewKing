"""Small WSGI login service. Deploy behind an HTTPS reverse proxy/WSGI server."""
import base64
import hashlib
import hmac
import io
import json
import os
from pathlib import Path
import secrets
import sqlite3
import threading
import time
from urllib.parse import urlencode
from urllib.request import Request, urlopen

TOKEN_URL = 'https://open.tiktokapis.com/v2/oauth/token/'

class LoginApp:
    def __init__(self, db_path, client_key, client_secret, identity_secret, allowed_origins=(), exchange=None, clock=time.time):
        self.db_path, self.client_key, self.client_secret = str(db_path), client_key, client_secret
        self.identity_secret = identity_secret
        self.allowed_origins = frozenset(allowed_origins)
        self.exchange, self.clock = exchange or self.exchange_tiktok, clock
        self.lock, self.rates = threading.Lock(), {}
        Path(self.db_path).parent.mkdir(parents=True, exist_ok=True)
        with self.db() as db:
            db.executescript('''CREATE TABLE IF NOT EXISTS users (player_id TEXT PRIMARY KEY, open_id TEXT UNIQUE NOT NULL);
                CREATE TABLE IF NOT EXISTS sessions (token_hash TEXT PRIMARY KEY, player_id TEXT NOT NULL, expires INTEGER NOT NULL);
                CREATE TABLE IF NOT EXISTS codes (code_hash TEXT PRIMARY KEY, created INTEGER NOT NULL);''')
        try: os.chmod(self.db_path, 0o600)
        except OSError: pass

    def db(self):
        return sqlite3.connect(self.db_path, timeout=10)

    @property
    def configured(self):
        return bool(self.client_key and self.client_secret and len(self.identity_secret) >= 32)

    def exchange_tiktok(self, code):
        body = urlencode({'client_key':self.client_key,'client_secret':self.client_secret,'code':code,'grant_type':'authorization_code'}).encode()
        req = Request(TOKEN_URL, data=body, headers={'Content-Type':'application/x-www-form-urlencoded','Cache-Control':'no-cache'})
        with urlopen(req, timeout=10) as response:
            payload = json.loads(response.read(32768))
        if payload.get('error') or not isinstance(payload.get('open_id'), str) or not payload['open_id']:
            raise ValueError('OAuth rejected')
        # No profile data is requested, and TikTok access/refresh tokens are not sent to the game or stored.
        return payload['open_id']

    def __call__(self, env, start_response):
        headers = [('Content-Type','application/json'),('Cache-Control','no-store'),('X-Content-Type-Options','nosniff')]
        origin = env.get('HTTP_ORIGIN','')
        if origin and origin in self.allowed_origins:
            headers += [('Access-Control-Allow-Origin',origin),('Vary','Origin')]
        def send(status, data):
            payload=json.dumps(data,separators=(',',':')).encode()
            start_response(status, headers+[('Content-Length',str(len(payload)))])
            return [payload]
        if origin and origin not in self.allowed_origins:
            return send('403 Forbidden',{'error':'origin_not_allowed'})
        method,path=env.get('REQUEST_METHOD','GET'),env.get('PATH_INFO','')
        if method=='OPTIONS':
            headers += [('Access-Control-Allow-Methods','POST, GET, OPTIONS'),('Access-Control-Allow-Headers','Content-Type, Authorization')]
            return send('200 OK',{})
        if path=='/health' and method=='GET':
            return send('200 OK',{'ok':True,'configured':self.configured})
        if path=='/v1/session' and method=='GET':
            token=env.get('HTTP_AUTHORIZATION','').removeprefix('Bearer ')
            hashed=hashlib.sha256(token.encode()).hexdigest()
            with self.db() as db:
                row=db.execute('SELECT player_id FROM sessions WHERE token_hash=? AND expires>?',(hashed,int(self.clock()))).fetchone()
            return send('200 OK',{'playerId':row[0]}) if row else send('401 Unauthorized',{'error':'session_expired'})
        if path!='/v1/tiktok/session' or method!='POST':
            return send('404 Not Found',{'error':'not_found'})
        if not self.configured:
            return send('503 Service Unavailable',{'error':'server_not_configured'})
        now=int(self.clock()); peer=env.get('REMOTE_ADDR','unknown') # Do not trust arbitrary X-Forwarded-For.
        with self.lock:
            self.rates={key:value for key,value in self.rates.items() if value[0]>now-60}
            window,count=self.rates.get(peer,(now,0))
            if now-window>=60: window,count=now,0
            if count>=30: return send('429 Too Many Requests',{'error':'rate_limited'})
            self.rates[peer]=(window,count+1)
        try:
            length=int(env.get('CONTENT_LENGTH','0'))
            if length<=0 or length>8192 or env.get('CONTENT_TYPE','').split(';')[0]!='application/json':
                return send('400 Bad Request',{'error':'invalid_request'})
            payload=json.loads(env['wsgi.input'].read(length)); code=payload.get('code')
            if not isinstance(code,str) or not code.strip() or len(code)>4096:
                return send('400 Bad Request',{'error':'invalid_request'})
        except (ValueError,TypeError,AttributeError):
            return send('400 Bad Request',{'error':'invalid_request'})
        code_hash=hashlib.sha256(code.encode()).hexdigest()
        with self.db() as db:
            db.execute('DELETE FROM codes WHERE created<?',(now-600,))
            db.execute('DELETE FROM sessions WHERE expires<=?',(now,))
            try: db.execute('INSERT INTO codes VALUES (?,?)',(code_hash,now))
            except sqlite3.IntegrityError: return send('409 Conflict',{'error':'login_code_already_used'})
        try:
            open_id=self.exchange(code)
            if not isinstance(open_id,str) or not open_id or len(open_id)>512: raise ValueError('Invalid identity')
        except Exception:
            return send('502 Bad Gateway',{'error':'tiktok_login_failed'})
        player_id=hmac.new(self.identity_secret.encode(),open_id.encode(),hashlib.sha256).hexdigest()
        token=secrets.token_urlsafe(32)
        with self.db() as db:
            db.execute('INSERT OR IGNORE INTO users VALUES (?,?)',(player_id,open_id))
            db.execute('INSERT INTO sessions VALUES (?,?,?)',(hashlib.sha256(token.encode()).hexdigest(),player_id,now+7*86400))
        return send('200 OK',{'playerId':player_id,'sessionToken':token})

def from_environment():
    return LoginApp(os.getenv('DATABASE_PATH','./data/tiktok.sqlite3'),os.getenv('TIKTOK_CLIENT_KEY',''),os.getenv('TIKTOK_CLIENT_SECRET',''),os.getenv('SESSION_ID_SECRET',''),filter(None,os.getenv('ALLOWED_ORIGINS','').split(',')))

if __name__=='__main__':
    from wsgiref.simple_server import make_server, WSGIRequestHandler
    class QuietHandler(WSGIRequestHandler):
        def log_message(self, fmt, *args): pass
    app=from_environment()
    print('Local login service: http://127.0.0.1:8781 (configure secrets before device testing)',flush=True)
    make_server('127.0.0.1',8781,app,handler_class=QuietHandler).serve_forever()
