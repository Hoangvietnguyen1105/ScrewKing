const fs=require('fs'),vm=require('vm'),path=require('path'),assert=require('assert');
const root=path.resolve(__dirname,'..'),src=path.join(root,'Assets/ScrewKing/Resources/Source');
const library={},heap=Buffer.alloc(16*1024*1024);let cursor=16,frees=0;
function malloc(n){const p=cursor;cursor+=n;assert(cursor<heap.length);return p;}
function encode(s){const p=malloc(Buffer.byteLength(s)+1);heap.write(s+'\0',p);return p;}
function decode(p){return heap.toString('utf8',p,heap.indexOf(0,p));}
const host=vm.createContext({LibraryManager:{library},mergeInto:Object.assign,UTF8ToString:decode,lengthBytesUTF8:Buffer.byteLength,_malloc:malloc,_free:()=>frees++,stringToUTF8:(s,p)=>heap.write(s+'\0',p)});
vm.runInContext(fs.readFileSync(path.join(root,'Assets/Plugins/WebGL/ScrewKingSource.jslib'),'utf8'),host);
host.ScrewKingSource=library.$ScrewKingSource;
const native=vm.createContext({});for(const n of ['CocosPhysics','CocosRules','Bridge'])vm.runInContext(fs.readFileSync(path.join(src,n+'.js.txt'),'utf8'),native);
const id=library.SKSourceCreate(),id2=library.SKSourceCreate();
function call(operation,...args){const reply=JSON.parse(host.ScrewKingSource.call(id,operation,JSON.stringify(args)));assert(reply.ok,reply.error);return reply.value;}
let runs=0;
for(let level=1;level<=89;level++)for(const seed of [12345,42]){
 const raw=fs.readFileSync(path.join(root,`Assets/ScrewKing/Resources/Data/Levels/${level}.json`),'utf8');native.raw=raw;native.seed=seed;
 assert.strictEqual(call('Generate',raw,seed),vm.runInContext('SourceGenerate(raw,seed)',native));runs++;
}
const raw=fs.readFileSync(path.join(root,'Assets/ScrewKing/Resources/Data/PhysicsOracleLevel.json'),'utf8');native.raw=raw;
call('Generate',raw,12345);vm.runInContext('SourceGenerate(raw,12345)',native);
call('ResetPhysics');call('AddBoard',0,0,0,-230);call('ShowBoard',0,true);
vm.runInContext('SourcePhysicsReset();SourceBoardAdd(0,0,0,-230);SourceBoardVisible(0,true)',native);
for(let i=0;i<36;i++){call('Step',1/60);vm.runInContext('SourcePhysicsStep(1/60)',native);}
call('Dynamic',0);for(let i=0;i<3;i++)call('Release',0,i);
vm.runInContext('SourceBoardDynamic(0);SourceRelease(0,0);SourceRelease(0,1);SourceRelease(0,2)',native);
for(let step=1;step<=180;step++){
 if(step===121){call('Release',0,3);vm.runInContext('SourceRelease(0,3)',native);}
 assert.strictEqual(call('Step',1/60),vm.runInContext('SourcePhysicsStep(1/60)',native));
}
assert(!JSON.parse(host.ScrewKingSource.call(id2,'Step','[0]')).ok,'contexts isolated');
assert(!JSON.parse(host.ScrewKingSource.call(id,'constructor','[]')).ok,'no prototype dispatch');
const pointer=library.SKSourceCall(id,encode('Pick'),encode('[[1,2,3]]'));assert(JSON.parse(decode(pointer)).ok);library.SKSourceFree(pointer);assert(frees===1);
library.SKSourceDestroy(id);library.SKSourceDestroy(id2);assert(Object.keys(host.ScrewKingSource.instances).length===0);
const report={passed:true,generation_runs:runs,levels:89,physics_steps:180,isolated_contexts:true,invalid_operation_rejected:true,utf8_boundary:true,cleanup:true,scope:'Node emulation of generated jslib; not an actual Unity WebGL or TikTok device test'};
fs.writeFileSync(path.join(__dirname,'source-webgl-verification.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
