import io,json,tempfile,unittest
from pathlib import Path
from server import LoginApp

class ServerTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.now=1000;self.codes=[]
        def exchange(code):
            self.codes.append(code)
            if code=='invalid': raise ValueError()
            return 'private-open-id'
        self.app=LoginApp(Path(self.tmp.name)/'db.sqlite','public-key','server-secret','s'*40,allowed_origins=['https://game.example'],exchange=exchange,clock=lambda:self.now)
    def tearDown(self):self.tmp.cleanup()
    def call(self,path='/v1/tiktok/session',method='POST',body=None,origin='',token=''):
        data=json.dumps(body if body is not None else {'code':'one-use-code'}).encode();answer={}
        env={'PATH_INFO':path,'REQUEST_METHOD':method,'CONTENT_LENGTH':str(len(data)),'CONTENT_TYPE':'application/json','wsgi.input':io.BytesIO(data),'REMOTE_ADDR':'127.0.0.1','HTTP_ORIGIN':origin,'HTTP_AUTHORIZATION':'Bearer '+token}
        result=b''.join(self.app(env,lambda status,headers:answer.update(status=status,headers=headers)))
        return int(answer['status'].split()[0]),json.loads(result),dict(answer['headers'])
    def test_login_and_session_no_platform_tokens(self):
        status,data,headers=self.call();self.assertEqual(status,200);self.assertEqual(set(data),{'playerId','sessionToken'});self.assertNotIn('private-open-id',json.dumps(data));self.assertEqual(headers['Cache-Control'],'no-store')
        status,result,_=self.call('/v1/session','GET',token=data['sessionToken']);self.assertEqual(status,200);self.assertEqual(result['playerId'],data['playerId'])
        self.now+=8*86400;self.assertEqual(self.call('/v1/session','GET',token=data['sessionToken'])[0],401)
    def test_replay_rejected_and_identity_stable(self):
        a=self.call()[1];self.assertEqual(self.call()[0],409);b=self.call(body={'code':'another-code'})[1];self.assertEqual(a['playerId'],b['playerId']);self.assertNotEqual(a['sessionToken'],b['sessionToken'])
    def test_invalid_and_unconfigured(self):
        self.assertEqual(self.call(body={'code':''})[0],400);self.assertEqual(self.call(body={'code':'invalid'})[0],502)
        self.app.client_secret='';self.assertEqual(self.call()[0],503)
    def test_origin_and_rate_limit(self):
        self.assertEqual(self.call(origin='https://untrusted.example')[0],403)
        for i in range(30):self.call(body={'code':f'code-{i}'})
        self.assertEqual(self.call(body={'code':'one-more'})[0],429)
    def test_persistence_across_restart(self):
        original=self.call()[1]
        self.app=LoginApp(self.app.db_path,'public-key','server-secret','s'*40,exchange=lambda code:'private-open-id',clock=lambda:self.now)
        self.assertEqual(self.call('/v1/session','GET',token=original['sessionToken'])[0],200)

if __name__=='__main__':unittest.main()
