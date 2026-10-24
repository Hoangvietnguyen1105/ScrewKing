"""Verify the copied base SDK is unchanged; this is not a live-platform test."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[1]
base=Path('/Users/hoangvietnguyen1105/GameFrameworkMini')
report={'source':str(base),'groups':{},'passed':True}
for name,relative in [('MoodBoost SDK','Assets/_Dev/Sdk'),('TTSDK','Assets/Plugins/com.tiktok.minigame'),('UniTask','Assets/Plugins/UniTask')]:
 files=[p for p in (base/relative).rglob('*') if p.is_file()]
 different=[]
 for p in files:
  dest=root/relative/p.relative_to(base/relative)
  if not dest.is_file() or hashlib.sha256(p.read_bytes()).digest()!=hashlib.sha256(dest.read_bytes()).digest():different.append(str(p.relative_to(base/relative)))
 report['groups'][name]={'files':len(files),'different':different}
 report['passed'] &= not different
lock=json.loads((root/'Packages/packages-lock.json').read_text())
report['vcontainer_commit']=lock['dependencies']['jp.hadashikick.vcontainer'].get('hash')
report['passed'] &= report['vcontainer_commit']=='5401e5a7ebc4980a2b82141ffc26391a6547edd7'
out=root.parent/'Builds/TikTok/sdk-base-integrity.json';out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report));raise SystemExit(0 if report['passed'] else 1)
