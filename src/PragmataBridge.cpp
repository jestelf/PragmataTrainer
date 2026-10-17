#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <cmath>
#include <cstdint>
#include <cstring>
#include <cstdio>
#include <string>
#include <vector>
#include <deque>
#include <unordered_map>
#include <map>
#include <set>
#include <mutex>
#include <atomic>
#include <sstream>
#include <algorithm>

static const char* kPipe = "\\\\.\\pipe\\PragmataTrainerBridge";
static const int kBridgeVersion = 1;

static bool Ok(const void* p, size_t n = 8) {
 if((uintptr_t)p < 0x10000 || (uintptr_t)p > 0x7FFFFFFFFFFF) return false;
 MEMORY_BASIC_INFORMATION mi; if(!VirtualQuery(p,&mi,sizeof mi)) return false;
 if(mi.State!=MEM_COMMIT || (mi.Protect&(PAGE_GUARD|PAGE_NOACCESS))) return false;
 if(!(mi.Protect&(PAGE_READONLY|PAGE_READWRITE|PAGE_WRITECOPY|PAGE_EXECUTE_READ|PAGE_EXECUTE_READWRITE|PAGE_EXECUTE_WRITECOPY))) return false;
 return (uintptr_t)p+n <= (uintptr_t)mi.BaseAddress+mi.RegionSize || Ok((void*)((uintptr_t)mi.BaseAddress+mi.RegionSize), (uintptr_t)p+n-((uintptr_t)mi.BaseAddress+mi.RegionSize));
}
template<class T> static T R(uintptr_t a){ T v{}; __try{ v=*(T*)a; }__except(EXCEPTION_EXECUTE_HANDLER){} return v; }
template<class T> static bool W(uintptr_t a,T v){ __try{ *(T*)a=v; return true; }__except(EXCEPTION_EXECUTE_HANDLER){ return false; } }

static uintptr_t gBase=0, gSize=0;
static int ScanRange(const uint8_t* p,const uint8_t* e,const int* pat,size_t n,uintptr_t* hits,int max){
 int got=0;
 __try{ for(;p+n<=e && got<max;p++){ size_t k=0; for(;k<n;k++){ if(pat[k]>=0 && p[k]!=pat[k]) break; } if(k==n) hits[got++]=(uintptr_t)p; } }__except(EXCEPTION_EXECUTE_HANDLER){}
 return got;
}
static std::vector<uintptr_t> Scan(const char* sig, int max=1){
 std::vector<int> pat; for(const char* s=sig;*s;){ if(*s==' '){s++;continue;} if(*s=='?'){pat.push_back(-1); while(*s=='?')s++; continue;} pat.push_back((int)strtoul(std::string(s,2).c_str(),nullptr,16)); s+=2; }
 std::vector<uintptr_t> out; uintptr_t a=gBase, end=gBase+gSize;
 while(a<end){
  MEMORY_BASIC_INFORMATION mi; if(!VirtualQuery((void*)a,&mi,sizeof mi)) break;
  uintptr_t rb=(uintptr_t)mi.BaseAddress, re=std::min(end,rb+mi.RegionSize);
  bool readable = mi.State==MEM_COMMIT && !(mi.Protect&(PAGE_GUARD|PAGE_NOACCESS)) && (mi.Protect&(PAGE_EXECUTE_READ|PAGE_EXECUTE_READWRITE|PAGE_READONLY|PAGE_READWRITE|PAGE_EXECUTE_WRITECOPY|PAGE_WRITECOPY));
  if(readable){
   uintptr_t hits[64]; const uint8_t* p=(const uint8_t*)std::max(rb,a);
   for(;;){ int left=max-(int)out.size(); int got=ScanRange(p,(const uint8_t*)re,pat.data(),pat.size(),hits,std::min(left,64)); for(int k=0;k<got;k++) out.push_back(hits[k]);
    if((int)out.size()>=max) return out; if(got<std::min(left,64)) break; p=(const uint8_t*)hits[got-1]+1; }
  }
  a=re;
 }
 return out;
}
static uintptr_t Rel(uintptr_t at){ return at+4+R<int32_t>(at); }

namespace tdb {
 uintptr_t vm=0, tdb=0; void** vmGlobal=nullptr; using CtxFn=void*(*)(void*,int); CtxFn getCtx=nullptr;
 int32_t tdbOff=0, staticOff=0; uintptr_t encBase=0;
 uint32_t nTypes=0,nMethods=0,nFields=0,nTypeImpl=0,nFieldImpl=0,nMethodImpl=0,nParams=0,version=0;
 uintptr_t types,typesImpl,methods,methodsImpl,fields,fieldsImpl,params,strings,bytes,initData;
 std::string status="не инициализирована";

 static uint64_t B(uint64_t v,int lo,int n){ return (v>>lo)&((1ull<<n)-1); }
 static uintptr_t Type(uint32_t i){ return types+0x50ull*i; }
 static uint32_t Parent(uint32_t i){ return (uint32_t)B(R<uint64_t>(Type(i)),19,19); }
 static uint32_t Decl(uint32_t i){ return (uint32_t)B(R<uint64_t>(Type(i)),38,19); }
 static uint32_t Impl(uint32_t i){ return (uint32_t)B(R<uint64_t>(Type(i)+8),38,18); }
 static uint32_t Size(uint32_t i){ return R<uint32_t>(Type(i)+0x14); }
 static uint32_t ObjKind(uint32_t i){ return (uint32_t)B(R<uint32_t>(Type(i)+0x2C),26,3); }
 static uint32_t MMethod(uint32_t i){ return (uint32_t)B(R<uint64_t>(Type(i)+0x20),22,22); }
 static uint32_t MField(uint32_t i){ return (uint32_t)B(R<uint64_t>(Type(i)+0x20),44,20); }
 static const char* Str(uint32_t off){ return (const char*)(strings+off); }
 static uintptr_t ImplP(uint32_t i){ uint32_t k=Impl(i); return k<nTypeImpl? typesImpl+0x30ull*k : 0; }
 static const char* Name(uint32_t i){ uintptr_t p=ImplP(i); return p? Str((uint32_t)B(R<uint64_t>(p),0,28)) : "?"; }
 static const char* Ns(uint32_t i){ uintptr_t p=ImplP(i); return p? Str((uint32_t)B(R<uint64_t>(p),28,28)) : ""; }
 static uint32_t NumFields(uint32_t i){ uintptr_t p=ImplP(i); return p? (uint32_t)B(R<uint64_t>(p+0x10),33,24) : 0; }
 static uint32_t NumMethods(uint32_t i){ uintptr_t p=ImplP(i); return p? R<uint16_t>(p+0x18) : 0; }

 static std::vector<std::string> gNames; static std::unordered_map<std::string,uint32_t> gByName;
 static const std::string& Full(uint32_t i,int depth=0){
  static std::string bad="?";
  if(i>=nTypes) return bad; std::string& s=gNames[i]; if(!s.empty()) return s;
  uint32_t d=Decl(i); std::string n=Name(i);
  if(d && d!=i && d<nTypes && depth<16) s=Full(d,depth+1)+"."+n; else { std::string ns=Ns(i); s=ns.empty()? n : ns+"."+n; }
  return s;
 }
 static void BuildNames(){
  if(!gByName.empty()) return; gNames.assign(nTypes,std::string());
  for(uint32_t i=1;i<nTypes;i++){ const std::string& n=Full(i); if(!gByName.count(n)) gByName[n]=i; }
 }
 static uint32_t Find(const std::string& name){ BuildNames(); auto it=gByName.find(name); return it==gByName.end()?0:it->second; }
 static bool IsA(uint32_t t,uint32_t base){ for(int k=0;t && k<64;k++){ if(t==base) return true; uint32_t p=Parent(t); if(p==t) break; t=p; } return false; }

 static uint32_t TypeOf(uintptr_t obj){
  if(!Ok((void*)obj,16)) return 0; uintptr_t info=R<uintptr_t>(obj); if(!Ok((void*)info)) return 0;
  uintptr_t td=R<uintptr_t>(info); if(td<types || td>=types+0x50ull*nTypes || (td-types)%0x50) return 0;
  return (uint32_t)((td-types)/0x50);
 }
 static int32_t FieldBase(uint32_t t){
  for(int k=0;t && k<64;k++){ uintptr_t vt=R<uintptr_t>(Type(t)+0x40); if(vt && Ok((void*)(vt-8),4)) return R<int32_t>(vt-8); uint32_t p=Parent(t); if(p==t) break; t=p; }
  return 0x10;
 }
 struct Field{ uint32_t decl=0, type=0; uint32_t off=0; uint16_t flags=0; bool ok=false; bool isStatic() const { return (flags&0x10)!=0; } };
 static Field FieldAt(uint32_t k){
  Field f; uint64_t q=R<uint64_t>(fields+8ull*k); f.decl=(uint32_t)B(q,0,19); uint32_t im=(uint32_t)B(q,19,19); f.type=(uint32_t)B(q,38,19);
  uintptr_t o=fieldsImpl+12ull*im; f.flags=R<uint16_t>(o+2)>>1; f.off=R<uint32_t>(o+4)&0x3FFFFFF; f.ok=true; return f;
 }
 static bool FieldLiteral(uint32_t k,uint32_t& v){
  uint64_t q=R<uint64_t>(fields+8ull*k); uint32_t im=(uint32_t)B(q,19,19); uintptr_t o=fieldsImpl+12ull*im;
  if(!((R<uint16_t>(o+2)>>1)&0x40)) return false;
  uint32_t a=R<uint32_t>(o+4), b=R<uint32_t>(o+8); uint32_t idx=(a>>26)|((b>>28)<<6)|((uint32_t)B(q,57,7)<<10);
  int32_t off=R<int32_t>(initData+4ull*idx); if(off<0) return false; v=R<uint32_t>(bytes+off); return true;
 }
 static const char* FieldName(uint32_t k){ uint64_t q=R<uint64_t>(fields+8ull*k); uint32_t im=(uint32_t)B(q,19,19); return Str(R<uint32_t>(fieldsImpl+12ull*im+8)&0xFFFFFFF); }
 static std::map<std::pair<uint32_t,std::string>,Field> gFieldCache;
 static Field FindField(uint32_t t,const std::string& name){
  auto key=std::make_pair(t,name); auto it=gFieldCache.find(key); if(it!=gFieldCache.end()) return it->second;
  Field r; uint32_t c=t;
  for(int d=0;c && d<64 && !r.ok;d++){
   uint32_t s=MField(c), n=NumFields(c);
   for(uint32_t k=s;k<s+n && k<nFields;k++) if(name==FieldName(k)){ r=FieldAt(k); r.decl=c; break; }
   uint32_t p=Parent(c); if(p==c) break; c=p;
  }
  gFieldCache[key]=r; return r;
 }
 struct Method{ uintptr_t fn=0; uint32_t decl=0; uint16_t flags=0; int np=0; bool ok=false; };
 static Method MethodAt(uint32_t k){
  Method m; uintptr_t o=methods+12ull*k; uint32_t a=R<uint32_t>(o), b=R<uint32_t>(o+4); int32_t enc=R<int32_t>(o+8);
  m.decl=a&0x7FFFF; uint32_t im=b&0x7FFFF; uint32_t prm=(a>>19)|((b>>19)<<13);
  m.flags=R<uint16_t>(methodsImpl+12ull*im+4); m.np=R<uint16_t>(bytes+prm);
  m.fn= (enc && encBase)? encBase+(intptr_t)enc : 0; m.ok=true; return m;
 }
 static const char* MethodName(uint32_t k){ uint32_t b=R<uint32_t>(methods+12ull*k+4); return Str(R<uint32_t>(methodsImpl+12ull*(b&0x7FFFF)+8)&0xFFFFFFF); }
 static std::map<std::string,Method> gMethodCache;
 static Method FindMethod(uint32_t t,const std::string& name,int np=-1){
  std::string key=std::to_string(t)+":"+name+":"+std::to_string(np); auto it=gMethodCache.find(key); if(it!=gMethodCache.end()) return it->second;
  Method r; uint32_t c=t;
  for(int d=0;c && d<64 && !r.ok;d++){
   uint32_t s=MMethod(c), n=NumMethods(c);
   for(uint32_t k=s;k<s+n && k<nMethods;k++) if(name==MethodName(k)){ Method m=MethodAt(k); if(np<0 || m.np==np){ r=m; break; } }
   uint32_t p=Parent(c); if(p==c) break; c=p;
  }
  gMethodCache[key]=r; return r;
 }
 static Method FindMethod(const char* type,const std::string& name,int np=-1){ uint32_t t=Find(type); return t? FindMethod(t,name,np) : Method(); }
 static uintptr_t StaticBase(uint32_t t){
  uintptr_t tbl=R<uintptr_t>(vm+staticOff); uint32_t n=R<uint32_t>(vm+staticOff+8);
  if(!tbl || t>=n) return 0; return R<uintptr_t>(tbl+8ull*t);
 }

 static bool Init(){

  static uintptr_t ref=0;
  if(!ref){ std::map<uintptr_t,int> cnt; for(uintptr_t a : Scan("48 8B 0D ?? ?? ?? ?? BA FF FF FF FF E8 ?? ?? ?? ??",4000)){ uintptr_t g=Rel(a+3); if(++cnt[g]>10){ ref=a; break; } } }
  if(!ref){ status="не найдена VM движка"; return false; }
  vmGlobal=(void**)Rel(ref+3); getCtx=(CtxFn)Rel(ref+13);
  vm=(uintptr_t)R<void*>((uintptr_t)vmGlobal); if(!vm){ status="VM ещё не создана"; return false; }
  auto filled=[&](uintptr_t t){ uint32_t n=R<uint32_t>(t+0x14); uintptr_t m=R<uintptr_t>(t+0x78); if(m && m<t) m+=t; if(!Ok((void*)m,12)) return 0; int c=0; for(uint32_t k=0;k<2000 && k<n;k++) if(R<int32_t>(m+12ull*k+8)) c++; return c; };
  for(int i=0;i<0x20000;i+=8){ uintptr_t p=R<uintptr_t>(vm+i); if(p && !(p&7) && Ok((void*)p,8) && R<uint32_t>(p)==0x424454){ if(!tdb){ tdb=p; tdbOff=i; } if(filled(p)){ tdb=p; tdbOff=i; break; } } }
  if(!tdb){ status="в VM нет TDB"; return false; }
  version=R<uint32_t>(tdb+4);
  if(version!=83){ status="неожиданная версия TDB "+std::to_string(version); return false; }
  nTypes=R<uint32_t>(tdb+8); nMethods=R<uint32_t>(tdb+0x14); nFields=R<uint32_t>(tdb+0x18); nTypeImpl=R<uint32_t>(tdb+0x1C);
  nFieldImpl=R<uint32_t>(tdb+0x20); nMethodImpl=R<uint32_t>(tdb+0x24); nParams=R<uint32_t>(tdb+0x34);
  uintptr_t p[18]; for(int i=0;i<18;i++){ p[i]=R<uintptr_t>(tdb+0x60+8*i); if(p[i] && p[i]<tdb) p[i]+=tdb; }
  types=p[1]; typesImpl=p[2]; methods=p[3]; methodsImpl=p[4]; fields=p[5]; fieldsImpl=p[6]; params=p[10]; initData=p[12]; strings=p[15]; bytes=p[16];
  if(!Ok((void*)types) || !Ok((void*)strings) || !Ok((void*)methods)){ status="таблицы TDB недоступны"; return false; }

  staticOff=tdbOff-0x30;
  auto good=[&](int off){ uintptr_t t=R<uintptr_t>(vm+off); uint32_t n=R<uint32_t>(vm+off+8); return t && !(t&7) && Ok((void*)t) && n>=2000 && n<9999999; };
  if(!good(staticOff)){
   bool f=false;
   for(int i=8;i<0x100 && !f;i+=8){ int o=tdbOff-i; if(!good(o)) continue; int prev=o-16; if(good(prev) && R<uint32_t>(vm+prev+8)==R<uint32_t>(vm+o+8)){ staticOff=prev; f=true; } }
   if(!f){ status="не найдена таблица статических полей"; return false; }
  }

  if(!encBase){ auto e=Scan("85 C9 75 03 33 C0 C3 48 63 C1 48 8D 0D ?? ?? ?? ?? 48 03 C1 C3"); if(!e.empty()) encBase=Rel(e[0]+13); }
  if(!encBase){ auto e=Scan("85 C9 74 0E 48 63 C9 48 8D 05 ?? ?? ?? ?? 48 01 C8 C3 31 C0 C3"); if(!e.empty()) encBase=Rel(e[0]+10); }
  if(!encBase){ status="не найден декодер адресов методов"; return false; }
  status="ok"; return true;
 }
 static void* Ctx(){ return (vm && getCtx)? getCtx((void*)vm,-1) : nullptr; }
}

static thread_local void* tCtx=nullptr;
static void* Ctx(){ return tCtx? tCtx : tdb::Ctx(); }
static std::map<std::pair<uint32_t,uint32_t>,int32_t> gBaseCache;
static std::map<std::pair<uint32_t,uint32_t>,std::pair<ULONGLONG,int32_t>> gBaseGuess;
static int32_t BaseFor(uintptr_t obj,uint32_t t,uint32_t decl){
 auto key=std::make_pair(t,decl); auto it=gBaseCache.find(key); if(it!=gBaseCache.end()) return it->second;
 ULONGLONG now=GetTickCount64(); auto g=gBaseGuess.find(key); if(g!=gBaseGuess.end() && now-g->second.first<5000) return g->second.second;
 int32_t def=tdb::FieldBase(decl); uint32_t s=tdb::MField(decl), n=tdb::NumFields(decl);
 int best=0; int32_t pick=def; bool tie=false;
 for(int32_t b=0x10;b<=0x80;b+=8){
  int sc=0;
  for(uint32_t k=s;k<s+n && k<tdb::nFields;k++){
   tdb::Field f=tdb::FieldAt(k); if(f.isStatic() || (f.off&7)) continue;
   uintptr_t v=R<uintptr_t>(obj+b+f.off); if(!v) continue; uint32_t vt=tdb::TypeOf(v);
   if(vt && tdb::IsA(vt,f.type)) sc+=2; else if(!vt && (v>>48)) sc-=1;
  }
  if(sc>best){ best=sc; pick=b; tie=false; } else if(sc==best && sc>0){ tie=true; }
 }
 if(best>=4 && !tie){ gBaseCache[key]=pick; gBaseGuess.erase(key); return pick; }
 gBaseGuess[key]=std::make_pair(now,def); return def;
}
static uintptr_t FA(uintptr_t obj,const char* name){
 uint32_t t=tdb::TypeOf(obj); if(!t) return 0; tdb::Field f=tdb::FindField(t,name); if(!f.ok || f.isStatic()) return 0;
 return obj+BaseFor(obj,t,f.decl)+f.off;
}
template<class T> static T Get(uintptr_t obj,const char* name,T def=T()){ uintptr_t a=FA(obj,name); return a? R<T>(a) : def; }
template<class T> static bool Set(uintptr_t obj,const char* name,T v){ uintptr_t a=FA(obj,name); return a && W<T>(a,v); }
static uintptr_t Obj(uintptr_t obj,const char* name){ uintptr_t v=Get<uintptr_t>(obj,name); return tdb::TypeOf(v)? v : 0; }
static bool Is(uintptr_t obj,const char* type){ uint32_t b=tdb::Find(type); return b && tdb::IsA(tdb::TypeOf(obj),b); }

template<class Ret,class... A> static bool SafeCall(Ret* out,uintptr_t fn,A... a){ __try{ Ret r=((Ret(*)(void*,A...))fn)(Ctx(),a...); if(out) *out=r; return true; }__except(EXCEPTION_EXECUTE_HANDLER){ return false; } }
template<class... A> static bool SafeCallV(uintptr_t fn,A... a){ __try{ ((void(*)(void*,A...))fn)(Ctx(),a...); return true; }__except(EXCEPTION_EXECUTE_HANDLER){ return false; } }
static uintptr_t MFn(uintptr_t obj,const char* name,int np){ uint32_t t=tdb::TypeOf(obj); return t? tdb::FindMethod(t,name,np).fn : 0; }
template<class Ret,class... A> static bool Call(Ret* out,uintptr_t obj,const char* name,A... a){ uintptr_t fn=MFn(obj,name,(int)sizeof...(A)); return fn && SafeCall<Ret>(out,fn,(void*)obj,a...); }
template<class... A> static bool CallV(uintptr_t obj,const char* name,A... a){ uintptr_t fn=MFn(obj,name,(int)sizeof...(A)); return fn && SafeCallV(fn,(void*)obj,a...); }
template<class Ret,class... A> static bool CallStatic(Ret* out,const char* type,const char* name,A... a){ uintptr_t fn=tdb::FindMethod(type,name,(int)sizeof...(A)).fn; return fn && SafeCall<Ret>(out,fn,a...); }
static bool StaticU32(const char* type,const char* name,uint32_t& v){
 uint32_t t=tdb::Find(type); if(!t) return false;
 uint32_t s0=tdb::MField(t), n=tdb::NumFields(t);
 for(uint32_t k=s0;k<s0+n && k<tdb::nFields;k++) if(!strcmp(tdb::FieldName(k),name) && tdb::FieldLiteral(k,v)) return true;
 tdb::Field f=tdb::FindField(t,name); if(!f.ok || !f.isStatic()) return false;
 uintptr_t b=tdb::StaticBase(f.decl); if(!b) return false; v=R<uint32_t>(b+f.off); return true;
}

static std::map<std::string,std::pair<uint32_t,uint32_t>> gSingletonField;
static uintptr_t Singleton(const char* type){
 auto it=gSingletonField.find(type);
 if(it==gSingletonField.end()){
  uint32_t t=tdb::Find(type); std::pair<uint32_t,uint32_t> loc{0,0};
  if(t) for(uint32_t k=0;k<tdb::nFields;k++){
   if(strcmp(tdb::FieldName(k),"_Instance")) continue; tdb::Field f=tdb::FieldAt(k);
   if(f.type==t && f.isStatic()){ loc={f.decl,f.off}; break; }
  }
  it=gSingletonField.emplace(type,loc).first;
 }
 if(!it->second.first) return 0; uintptr_t b=tdb::StaticBase(it->second.first); if(!b) return 0;
 uintptr_t o=R<uintptr_t>(b+it->second.second); return tdb::TypeOf(o)? o : 0;
}

static std::vector<uintptr_t> ArrayRefs(uintptr_t arr){
 std::vector<uintptr_t> v; if(!tdb::TypeOf(arr)) return v; int n=R<int32_t>(arr+0x1C); if(n<0||n>100000) return v;
 for(int i=0;i<n;i++) v.push_back(R<uintptr_t>(arr+0x20+8ull*i)); return v;
}
static std::vector<uint32_t> ArrayU32(uintptr_t arr){
 std::vector<uint32_t> v; if(!tdb::TypeOf(arr)) return v; int n=R<int32_t>(arr+0x1C); if(n<0||n>100000) return v;
 for(int i=0;i<n;i++) v.push_back(R<uint32_t>(arr+0x20+4ull*i)); return v;
}
static std::vector<uintptr_t> ListItems(uintptr_t list){
 std::vector<uintptr_t> v; if(!list) return v; uintptr_t items=Get<uintptr_t>(list,"_items"); int n=Get<int32_t>(list,"_size");
 if(n<=0 || n>100000) return v; auto all=ArrayRefs(items); for(int i=0;i<n && i<(int)all.size();i++) if(tdb::TypeOf(all[i])) v.push_back(all[i]); return v;
}

static uintptr_t UpdaterFromHandle(uintptr_t h){ uintptr_t core=Obj(h,"_Core"); return core? Obj(core,"<Updater>k__BackingField") : 0; }
static uintptr_t Player(){
 uintptr_t cm=Singleton("app.CharacterManager"); if(!cm) return 0;
 uintptr_t h=Obj(cm,"CachedPlayerHandle"); uintptr_t u=h? UpdaterFromHandle(h) : 0;
 if(!u){ uintptr_t r=0; if(Call(&r,cm,"getPlayerHandle") && tdb::TypeOf(r)) u=UpdaterFromHandle(r); }
 return Is(u,"app.PlayerUpdater")? u : 0;
}
static uintptr_t GameObjectOf(uintptr_t component){ uintptr_t g=R<uintptr_t>(component+0x10); return tdb::TypeOf(g)? g : 0; }
static uintptr_t FindComponent(uintptr_t go,uint32_t base){
 uintptr_t c=R<uintptr_t>(go+0x18);
 for(int k=0;c && k<256;k++){ if(tdb::IsA(tdb::TypeOf(c),base)) return c; c=R<uintptr_t>(c+0x18); }
 return 0;
}
static std::vector<uintptr_t> gEnemies; static ULONGLONG gEnemiesAt=0; static std::string gEnemyNote;
static std::vector<uintptr_t>& Enemies(){
 ULONGLONG now=GetTickCount64(); if(now-gEnemiesAt<500) return gEnemies; gEnemiesAt=now; gEnemies.clear();
 uintptr_t cm=Singleton("app.CharacterManager"); if(!cm){ gEnemyNote="нет CharacterManager"; return gEnemies; }
 uint32_t enemyT=tdb::Find("app.EnemyUpdater"), handleT=tdb::Find("app.CharacterHandle"), ctxT=tdb::Find("app.Context"), updT=tdb::Find("app.CharacterUpdater");
 std::set<uintptr_t> seen; int values=0;
 for(const char* db : {"<ActiveContextDB>k__BackingField","<CharacterContextDB>k__BackingField"}){
  uintptr_t dict=Obj(cm,db); if(!dict) continue; uintptr_t entries=Get<uintptr_t>(dict,"_entries"); int count=Get<int32_t>(dict,"_count");
  if(!tdb::TypeOf(entries) || count<=0) continue; int n=std::min(count,R<int32_t>(entries+0x1C));
  uint32_t et=tdb::TypeOf(entries); (void)et;
  uint32_t esz=24; int best=-1;
  for(uint32_t cand : {24u,32u,40u,48u}){ int score=0; for(int i=0;i<std::min(n,16);i++){ uintptr_t e=entries+0x20+(uintptr_t)cand*i; for(uint32_t off=8;off+8<=cand;off+=8) if(tdb::TypeOf(R<uintptr_t>(e+off))) score++; }
   score-= (int)cand/8; if(score>best){ best=score; esz=cand; } }
  for(int i=0;i<n;i++){
   uintptr_t e=entries+0x20+(uintptr_t)esz*i; if(R<int32_t>(e+4)<-1) continue;
   for(uint32_t off=8;off+8<=esz;off+=8){
    uintptr_t v=R<uintptr_t>(e+off); uint32_t vt=tdb::TypeOf(v); if(!vt) continue; values++;
    uintptr_t upd=0;
    if(tdb::IsA(vt,updT)) upd=v;
    else if(tdb::IsA(vt,handleT)) upd=UpdaterFromHandle(v);
    else if(tdb::IsA(vt,ctxT)){ uintptr_t go=Obj(v,"<GameObject>k__BackingField"); if(go) upd=FindComponent(go,enemyT); }
    if(upd && tdb::IsA(tdb::TypeOf(upd),enemyT) && seen.insert(upd).second) gEnemies.push_back(upd);
   }
  }
 }
 gEnemyNote="врагов: "+std::to_string(gEnemies.size());
 return gEnemies;
}
static uintptr_t HitPoint(uintptr_t upd){ return Obj(upd,"<HitPoint>k__BackingField"); }

static bool GoGetScale(uintptr_t go,float& v){ float r=0; if(Call(&r,go,"get_TimeScale")){ v=r; return true; } return false; }
static bool GoSetScale(uintptr_t go,float v){ return CallV(go,"set_TimeScale",v); }

struct JumpScale{ uintptr_t at=0,vec=0,dir=0; float base=1.f,acc=9.80665f; }; static JumpScale gJump;
static std::map<std::string,bool> gOn; static std::map<std::string,float> gMul={{"speed",2.f},{"hackdmg",3.f},{"highjump",2.f}};
static std::string gLastTick;
struct Saved{ std::map<uintptr_t,uint8_t> b; std::map<uintptr_t,float> f; std::map<uintptr_t,int32_t> i; };
static std::map<std::string,Saved> gSaved;
template<class T> static void Keep(const std::string& key,uintptr_t a,T v);
template<> void Keep<uint8_t>(const std::string& key,uintptr_t a,uint8_t v){ if(!a) return; auto& m=gSaved[key].b; if(!m.count(a)) m[a]=R<uint8_t>(a); W<uint8_t>(a,v); }
template<> void Keep<float>(const std::string& key,uintptr_t a,float v){ if(!a) return; auto& m=gSaved[key].f; if(!m.count(a)) m[a]=R<float>(a); W<float>(a,v); }
template<> void Keep<int32_t>(const std::string& key,uintptr_t a,int32_t v){ if(!a) return; auto& m=gSaved[key].i; if(!m.count(a)) m[a]=R<int32_t>(a); W<int32_t>(a,v); }
static int32_t Orig(const std::string& key,uintptr_t a){ auto& m=gSaved[key].i; if(!m.count(a)) m[a]=R<int32_t>(a); return m[a]; }
static void Restore(const std::string& key){ auto& s=gSaved[key]; for(auto& kv:s.b) W(kv.first,kv.second); for(auto& kv:s.f) W(kv.first,kv.second); for(auto& kv:s.i) W(kv.first,kv.second); gSaved.erase(key); }
static std::map<uintptr_t,float> gScaleSaved[2];
static void RestoreScales(int k){ for(auto& kv:gScaleSaved[k]) if(tdb::TypeOf(kv.first)) GoSetScale(kv.first,kv.second); gScaleSaved[k].clear(); }

static void FillGauge(const std::string& key,uintptr_t g){
 if(!g) return; float tot=Get<float>(g,"<TotalPoint>k__BackingField"); if(tot>0) Set<float>(g,"<RemainingPoint>k__BackingField",tot);
 Keep<uint8_t>(key,FA(g,"<NoSub>k__BackingField"),1);
}
static void CompleteTimer(uintptr_t t){ if(!t) return; float lim=Get<float>(t,"_TimeLimit"); if(lim>0) Set<float>(t,"_TransitTime",lim); }

static std::vector<uintptr_t> PlayerWeapons(uintptr_t pl){ uintptr_t d=Obj(pl,"<PlayerItemControlDriver>k__BackingField"); return d? ListItems(Obj(d,"<Weapons>k__BackingField")) : std::vector<uintptr_t>(); }

static std::vector<uintptr_t> InventoryEntries(uintptr_t inv){
 std::vector<uintptr_t> out; for(const char* tbl : {"_ItemSlotTableMainGame","_ItemSlotTableTemporary"}){ uintptr_t t=Obj(inv,tbl); if(t) for(uintptr_t e:ListItems(Obj(t,"List"))) out.push_back(e); }
 return out;
}
static int AddItem(uintptr_t inv,uint32_t id,int n,int cap=0){
 int before=0; Call(&before,inv,"getItemQuantity",id);
 for(uintptr_t e:InventoryEntries(inv)){ if(Get<uint32_t>(e,"_ID")!=id) continue; int q=Get<int32_t>(e,"_Quantity"); int v=cap>0? cap : q+n; if(v<q) return 0;
  if(!CallV(e,"setQuantity",v)) Set<int32_t>(e,"_Quantity",v); int after=0; Call(&after,inv,"getItemQuantity",id); return after-before; }
 return 0;
}
static ULONGLONG gSlowAt=0;

static bool IsGameWindow(HWND w){ DWORD pid=0; GetWindowThreadProcessId(w,&pid); return pid==GetCurrentProcessId(); }
struct NoClip{ bool on=false; uintptr_t cc=0, tr=0, gw=0; float pos[3]={0,0,0}; ULONGLONG last=0; }; static NoClip gClip;
static bool Key(int vk){ return (GetAsyncKeyState(vk)&0x8000)!=0; }
static void NoClipTick(uintptr_t pl,std::ostringstream& st){
 uintptr_t ta=Obj(pl,"<TerrainAdsorber>k__BackingField"); uintptr_t go=GameObjectOf(pl); uintptr_t tr=0; if(go) Call(&tr,go,"get_Transform");
 uintptr_t cc=Obj(pl,"<CharacterController>k__BackingField"); uintptr_t gw=ta? Obj(ta,"_GravityWork") : 0; uintptr_t pp=ta? FA(ta,"<PrevPosition>k__BackingField") : 0;
 if(!ta || !tdb::TypeOf(tr) || !cc || !gw || !pp){ st<<"noclip: нет объектов "; return; }
 ULONGLONG now=GetTickCount64();
 if(!gClip.on || gClip.cc!=cc){ gClip.on=true; gClip.cc=cc; gClip.tr=tr; gClip.gw=gw; for(int i=0;i<3;i++) gClip.pos[i]=R<float>(pp+4*i); gClip.last=now; }
 CallV(cc,"set_Enabled",false); Set<uint8_t>(gw,"_EnabledGravity",0);
 float dt=(float)(now-gClip.last)/1000.f; if(dt>0.1f) dt=0.1f; gClip.last=now;
 float f[3]={0,0,0}; uintptr_t cam=Obj(pl,"<PlayerCamera>k__BackingField"); uintptr_t cq=cam? FA(cam,"<CameraObjectRotation>k__BackingField") : 0;
 if(cq){ float x=R<float>(cq),y=R<float>(cq+4),z=R<float>(cq+8),w=R<float>(cq+12); f[0]=-2.f*(x*z+w*y); f[1]=-2.f*(y*z-w*x); f[2]=-(1.f-2.f*(x*x+y*y)); }
 float len=sqrtf(f[0]*f[0]+f[1]*f[1]+f[2]*f[2]); if(len<0.01f){ f[0]=0; f[1]=0; f[2]=1; len=1; } for(int i=0;i<3;i++) f[i]/=len;
 float r[3]={f[2],0.f,-f[0]}; float rl=sqrtf(r[0]*r[0]+r[2]*r[2]); if(rl>0.01f){ r[0]/=rl; r[2]/=rl; }
 bool fg=GetForegroundWindow() && IsGameWindow(GetForegroundWindow());
 float mf=fg? (float)(Key('W')-Key('S')) : 0.f, mr=fg? (float)(Key('A')-Key('D')) : 0.f, mu=fg? (float)(Key(VK_SPACE)-Key(VK_CONTROL)) : 0.f;
 float speed=(fg && Key(VK_SHIFT))? 24.f : 8.f;
 for(int i=0;i<3;i++) gClip.pos[i]+=(f[i]*mf+r[i]*mr+(i==1? mu : 0.f))*speed*dt;
 alignas(16) static float v[4]; v[0]=gClip.pos[0]; v[1]=gClip.pos[1]; v[2]=gClip.pos[2]; v[3]=0.f;
 CallV(tr,"set_Position",(void*)v);
 uintptr_t ov=Obj(ta,"_OriginVelocity"); uintptr_t va=ov? FA(ov,"_MovingVelocity") : 0; if(va) for(int i=0;i<3;i++) W<float>(va+4*i,0.f);
 st<<"noclip "<<(int)gClip.pos[0]<<","<<(int)gClip.pos[1]<<","<<(int)gClip.pos[2]<<" ";
}
static void NoClipOff(){
 if(gClip.cc){ CallV(gClip.cc,"set_Enabled",true); CallV(gClip.cc,"warp"); }
 if(gClip.gw) Set<uint8_t>(gClip.gw,"_EnabledGravity",1);
 gClip=NoClip();
}
static void Tick(){
 ULONGLONG now=GetTickCount64(); bool slow=now-gSlowAt>=1000; if(slow) gSlowAt=now;
 bool any=false; for(auto& kv:gOn) any|=kv.second; if(!any){ gLastTick="ожидание"; return; }
 uintptr_t pl=Player(); std::ostringstream st;
 if(!pl){ gLastTick="игрок не найден (загрузите сохранение)"; return; }

 if(gOn["god"]){ uintptr_t hp=HitPoint(pl); if(hp){ Keep<uint8_t>("god",FA(hp,"<NoDamage>k__BackingField"),1); Keep<uint8_t>("god",FA(hp,"<NoDie>k__BackingField"),1);
   int def=Get<int32_t>(hp,"<DefaultHitPoint>k__BackingField"); if(def>0 && Get<int32_t>(hp,"_CurrentHitPoint")<def) Set<int32_t>(hp,"_CurrentHitPoint",def); st<<"hp="<<Get<int32_t>(hp,"_CurrentHitPoint")<<"/"<<def<<" "; } }
 if(gOn["nohit"]){ uintptr_t hp=HitPoint(pl); if(hp){ Keep<uint8_t>("nohit",FA(hp,"<Invincible>k__BackingField"),1); Keep<uint8_t>("nohit",FA(hp,"<NoDamage>k__BackingField"),1); } }

 if(gOn["jetpack"]){ uintptr_t d=Obj(pl,"<PlayerThrusterGaugeDriver>k__BackingField"); FillGauge("jetpack",d? Obj(d,"<BaseThrusterGauge>k__BackingField") : 0); }
 if(gOn["noclip"]) NoClipTick(pl,st);
 if(gOn["highjump"]){ uintptr_t ta=Obj(pl,"<TerrainAdsorber>k__BackingField"); uintptr_t gw=ta? Obj(ta,"_GravityWork") : 0;
  uintptr_t ov=ta? Obj(ta,"_OriginVelocity") : 0; uintptr_t sw=ta? Obj(ta,"_SubStepAdditionalVelocityWork") : 0;
  uintptr_t va=ov? FA(ov,"_MovingVelocity") : 0;
  if(gw && va && sw){ static bool wasRising=false; bool rising=Get<uint8_t>(gw,"_IsRising")!=0; float vy=R<float>(va+4); float k=sqrtf(gMul["highjump"])-1.f;
   if(rising && !wasRising && vy>0.5f && vy<40.f && k>0.f){ alignas(16) static float add[4]; add[0]=0.f; add[1]=vy*k; add[2]=0.f; add[3]=0.f; CallV(sw,"addMovingVelocity",(void*)add); }
   wasRising=rising;
   st<<"прыжок "<<(rising?"вверх":"вниз")<<" v="<<vy<<" "; } }

 if(gOn["oxygen"]){ uintptr_t cd=Obj(pl,"_PlayerCoreData"); if(cd){ Keep<float>("oxygen",FA(cd,"_SubOxygenGauge"),0.f); Keep<float>("oxygen",FA(cd,"_ReduceHpOnEmptyOxgenGauge"),0.f); } }

 if(gOn["repair"] && slow){
  uintptr_t inv=Singleton("app.InventoryManager"), en=Singleton("app.EnhanceManager"); int cap=0; uint32_t med=0;
  if(inv && en && Call(&cap,en,"getRepairKitCapacity") && cap>0 && StaticU32("app.ObjectIDs.Variety","Medicine",med)){
   uintptr_t arr=0; if(CallStatic(&arr,"app.ObjectIDs","getItemHashes",med)) for(uint32_t id:ArrayU32(arr)){ int q=0; if(Call(&q,inv,"getItemQuantity",id) && q>0 && q<cap) AddItem(inv,id,cap-q,cap); }
  }
 }

 if(gOn["speed"]){ uintptr_t go=GameObjectOf(pl); if(go){ if(!gScaleSaved[0].count(go)){ float v=-1; if(GoGetScale(go,v)) gScaleSaved[0][go]=v; } GoSetScale(go,gMul["speed"]); } }

 if(gOn["ammo"]||gOn["noreload"]||gOn["nocool"]||gOn["accuracy"]||gOn["norecoil"]){
  int w=0;
  for(uintptr_t wp:PlayerWeapons(pl)){ w++;
   if(gOn["ammo"]||gOn["noreload"]){ int mx=Get<int32_t>(wp,"_MaxRemainBulletNum"); if(mx>0 && Get<int32_t>(wp,"_CurrentRemainingBulletNum")<mx) Set<int32_t>(wp,"_CurrentRemainingBulletNum",mx); }
   if(gOn["noreload"]){ uintptr_t rc=Obj(wp,"_WeaponReloadController"); if(rc) CompleteTimer(Obj(rc,"_ReloadTimer"));
    uintptr_t rcc=Obj(wp,"_WeaponReloadChargeController"); if(rcc){ CompleteTimer(Obj(rcc,"_StartReloadChargeTimer")); CompleteTimer(Obj(rcc,"_ReloadChargeTimer")); }
    uintptr_t s=Obj(wp,"<CurrentStatus>k__BackingField"); if(s){ Set<float>(s,"<CurrentHeatGauge>k__BackingField",0.f); Set<uint8_t>(s,"<OverHeat>k__BackingField",0); } }
   if(gOn["nocool"]){ uintptr_t cc=Obj(wp,"_WeaponCoolTimeController"); if(cc) CompleteTimer(Obj(cc,"_CoolTimer")); }
   uintptr_t prm=(gOn["accuracy"]||gOn["norecoil"])? Obj(wp,"_WeaponParameter") : 0;
   if(prm && gOn["accuracy"]){ Keep<float>("accuracy",FA(prm,"_HoldShotSpreadDegree"),0.f); Keep<float>("accuracy",FA(prm,"_DirectlyShotSpreadDegree"),0.f); }
   if(prm && gOn["norecoil"]){ uintptr_t rp=Obj(prm,"_CameraRecoilParam");
    if(rp) for(const char* f : {"_PitchRangeDeg","_YawRangeDeg"}){ uintptr_t a=FA(rp,f); if(a){ Keep<float>("norecoil",a,0.f); Keep<float>("norecoil",a+4,0.f); } } }
  }
  if(gOn["norecoil"]){ uintptr_t cam=Obj(pl,"<PlayerCamera>k__BackingField"); uintptr_t ru=cam? Obj(cam,"_CameraRecoilUpdater") : 0; uintptr_t rq=ru? Obj(ru,"_Request") : 0;
   if(rq){ Set<float>(rq,"<Yaw>k__BackingField",0.f); Set<float>(rq,"<Pitch>k__BackingField",0.f); } }
  if((gOn["ammo"]||gOn["noreload"]) && slow){
   uintptr_t inv=Singleton("app.InventoryManager");
   if(inv) for(const char* tbl : {"_GadgetSlotTableMainGame","_GadgetSlotTableTemporary"}){ uintptr_t t=Obj(inv,tbl); if(!t) continue;
    for(uintptr_t g:ListItems(Obj(t,"List"))){ uint64_t h=0; int mx=0; if(Call(&h,g,"getHash") && Call(&mx,inv,"getGadgetMaxRemainingBulletNum",h) && mx>0) Set<int32_t>(g,"_RemainingBulletNum",mx); } }
  }
  st<<"оружие="<<w<<" ";
 }

 uintptr_t pz=Obj(pl,"<PlayerPuzzleControlDriver>k__BackingField");
 if(pz){
  if(gOn["hackgauge"]) FillGauge("hackgauge",Obj(pz,"_HackingGauge"));
  if(gOn["hackcool"]){ uintptr_t cu=Obj(pz,"_HackingCoolTimeUnit"); if(cu && !CallV(cu,"forceFinish")) CompleteTimer(Obj(cu,"_CoolTime")); }
  if(gOn["hacktimer"]){ std::vector<uintptr_t> units; for(const char* f : {"_PuzzlingTarget","_OverridePuzzlingTarget"}){ uintptr_t u=Obj(pz,f); if(u) units.push_back(u); }
   uintptr_t hm=Singleton("app.HackingManager"); if(hm){ uintptr_t u=Obj(hm,"<LastHackingTarget>k__BackingField"); if(u) units.push_back(u);
    for(uintptr_t d:ListItems(Obj(hm,"_PuzzleDriverList"))) for(uintptr_t u2:ArrayRefs(Get<uintptr_t>(d,"<Units>k__BackingField"))) if(tdb::TypeOf(u2)) units.push_back(u2); }
   int frozen=0; for(uintptr_t u:units){ uintptr_t pb=Obj(u,"_MyPuzzle"); if(!pb) continue;
    uintptr_t tm=Obj(pb,"_Timer"); if(!tm) continue;
    Keep<uint8_t>("hacktimer",FA(tm,"<isPause>k__BackingField"),1); Set<float>(tm,"_TransitTime",0.f); frozen++; }
   st<<"таймеров взлома: "<<frozen<<" "; }
  if(gOn["hack"]) for(const char* f : {"_PuzzlingTarget","_OverridePuzzlingTarget"}){ uintptr_t u=Obj(pz,f); uintptr_t pb=u? Obj(u,"_MyPuzzle") : 0; if(pb) Set<uint8_t>(pb,"_RequestForceSuccess",1); }
 }
 if(gOn["hackdmg"]){ uintptr_t cd=Obj(pl,"_PlayerCoreData"); float m=gMul["hackdmg"];
  if(cd) for(const char* f : {"_HackingDamage","_AddHackingDamageWhenOpen","_AddHackingDamageWhenMix","_AddHackingDamageWhenShock"}){ uintptr_t a=FA(cd,f); if(a){ int o=Orig("hackdmg",a); W<int32_t>(a,(int32_t)(o*m+0.5f)); } } }

 if(gOn["skills"] && slow){ uintptr_t en=Singleton("app.EnhanceManager"); if(en) CallV(en,"recoveryAllActiveSkill"); }

 if(gOn["invis"]||gOn["onehit"]||gOn["freeze"]){
  auto& es=Enemies(); int alive=0;
  for(uintptr_t e:es){
   if(!tdb::TypeOf(e)) continue; alive++;
   if(gOn["invis"]){ uintptr_t fm=Obj(e,"<FindManager>k__BackingField"); if(fm){ if(!Get<uint8_t>(fm,"_IsDisableTargetFind")){ if(!CallV(fm,"setDisableTargetFind",true)) Set<uint8_t>(fm,"_IsDisableTargetFind",1); }
     if(Get<uint8_t>(fm,"_FindTarget")) CallV(fm,"forceLostPlayer"); } }
   if(gOn["onehit"]){ uintptr_t hp=HitPoint(e); if(hp){ int c=Get<int32_t>(hp,"_CurrentHitPoint"); if(c>1) Set<int32_t>(hp,"_CurrentHitPoint",1); } }
   if(gOn["freeze"]){ uintptr_t go=GameObjectOf(e); if(go){ if(!gScaleSaved[1].count(go)){ float v=-1; if(GoGetScale(go,v)) gScaleSaved[1][go]=v; } GoSetScale(go,0.f); } }
  }
  st<<gEnemyNote<<" ";
 }
 gLastTick=st.str();
}

static void SwitchOff(const std::string& k){
 if(k=="noclip"){ NoClipOff(); return; }
 if(k=="highjump"){ gJump=JumpScale(); return; }
 if(k=="speed"){ RestoreScales(0); return; }
 if(k=="freeze"){ RestoreScales(1); return; }
 if(k=="invis"){ for(uintptr_t e:Enemies()){ uintptr_t fm=Obj(e,"<FindManager>k__BackingField"); if(fm && !CallV(fm,"setDisableTargetFind",false)) Set<uint8_t>(fm,"_IsDisableTargetFind",0); } return; }
 Restore(k);
}

static const char* kKeys[]={"god","nohit","jetpack","highjump","noclip","oxygen","repair","invis","speed","ammo","noreload","nocool","accuracy","norecoil","onehit","hack","hacktimer","hackgauge","hackcool","hackdmg","skills","freeze"};
static std::string Hex(uintptr_t v){ char b[32]; sprintf_s(b,"0x%llX",(unsigned long long)v); return b; }

static std::string Exec(const std::string& line){
 std::istringstream in(line); std::string cmd; in>>cmd;
 try{
  if(cmd=="hello"||cmd=="state"){
   std::ostringstream o; o<<"ok bridge="<<kBridgeVersion<<" tdb="<<tdb::status;
   uintptr_t pl=Player(); o<<" player="<<Hex(pl)<<" | "<<gLastTick;
   if(cmd=="hello"){ o<<"\nvm="<<Hex(tdb::vm)<<" tdbptr="<<Hex(tdb::tdb)<<" types="<<tdb::nTypes<<" methods="<<tdb::nMethods<<" enc="<<Hex(tdb::encBase)<<" static="<<tdb::staticOff; }
   return o.str();
  }
  if(cmd=="set"){ std::string k; int v=0; in>>k>>v; bool known=false; for(auto x:kKeys) known|=k==x; if(!known) return "err неизвестная функция";
   bool was=gOn[k]; gOn[k]=v!=0; if(was && !v) SwitchOff(k);
   if(v && !Player()) return "ok включено (игрок появится после загрузки)";
   return "ok"; }
  if(cmd=="mul"){ std::string k; float v=1; in>>k>>v; if(v<=0||v>100) return "err значение"; gMul[k]=v; return "ok"; }
  if(cmd=="heal"){ uintptr_t pl=Player(); if(!pl) return "err игрок не найден"; if(CallV(pl,"recoveryHitPointFull")) return "ok";
   uintptr_t hp=HitPoint(pl); int def=Get<int32_t>(hp,"<DefaultHitPoint>k__BackingField"); return def>0 && Set<int32_t>(hp,"_CurrentHitPoint",def)? "ok":"err здоровье недоступно"; }
  if(cmd=="give"){ std::string a; int n=0; in>>a>>n; uint32_t id=(uint32_t)std::stoul(a,nullptr,0); uintptr_t inv=Singleton("app.InventoryManager"); if(!inv) return "err инвентарь не найден";
   if(n<=0||n>10000000) return "err количество"; int before=0,after=0; Call(&before,inv,"getItemQuantity",id); int got=AddItem(inv,id,n); Call(&after,inv,"getItemQuantity",id);
   std::ostringstream o; o<<(got>0?"ok ":"err не добавлено: ")<<before<<" -> "<<after<<(got>0?"":" (записи предмета нет в инвентаре)"); return o.str(); }
  if(cmd=="blueprints"){ int w=0,pk=0;
   uintptr_t wm=Singleton("app.WeaponManager"); uintptr_t lst=0;
   if(wm && Call(&lst,wm,"getCurrentPurchaseWeaponsImpl")) for(uintptr_t e:ListItems(lst)) if(!Get<uint8_t>(e,"_Unlock") && Set<uint8_t>(e,"_Unlock",1)) w++;
   uintptr_t en=Singleton("app.EnhanceManager"); uintptr_t ep=en? Obj(en,"_EquipmentPerkMainGame") : 0;
   if(en && ep) for(uintptr_t pd:ListItems(Obj(en,"<PerkDatas>k__BackingField"))){ uintptr_t sel=Obj(pd,"_PerkSelector"); uint32_t id=sel? Get<uint32_t>(sel,"_ID") : 0; uintptr_t u=0;
    if(id && Call(&u,ep,"getPossessionPerkUnitImpl",id) && tdb::TypeOf(u) && !Get<uint8_t>(u,"_Unlock") && Set<uint8_t>(u,"_Unlock",1)) pk++; }
   if(!wm && !en) return "err менеджеры не найдены (загрузите сохранение)";
   return "ok оружие: +"+std::to_string(w)+" · перки: +"+std::to_string(pk); }
  if(cmd=="perks"){ uintptr_t en=Singleton("app.EnhanceManager"); if(!en) return "err EnhanceManager не найден";
   int n=0,lv=0; for(uintptr_t pd:ListItems(Obj(en,"<PerkDatas>k__BackingField"))){ uintptr_t sel=Obj(pd,"_PerkSelector"); if(!sel) continue; uint32_t id=Get<uint32_t>(sel,"_ID"); if(!id) continue;
    bool un=false; Call(&un,en,"isUnlockPerk",id); if(!un && CallV(en,"unlockPerk",id)) n++;
    int mx=0; if(Call(&mx,pd,"getMaxReinforceLevel") && mx>0){ int cur=0; Call(&cur,en,"getPerkLevel",id); if(cur<mx && CallV(en,"setPerkLevel",id,mx)) lv++; } }
   return "ok открыто перков: "+std::to_string(n)+", прокачано: "+std::to_string(lv); }

 }catch(...){ return "err исключение"; }
 return "err неизвестная команда";
}

struct Job{ std::string in,out; HANDLE done; };
static std::mutex gLock; static std::deque<Job*> gJobs; static std::atomic<int> gPending{0};
static DWORD gGameThread=0; static std::string gHookInfo="нет"; static bool gReady=false;
static void DoJob(Job* j){ j->out=Exec(j->in); }
static bool TryJob(Job* j){ __try{ DoJob(j); return true; }__except(EXCEPTION_EXECUTE_HANDLER){ return false; } }
static void RunJobs(){
 std::deque<Job*> run; { std::lock_guard<std::mutex> g(gLock); run.swap(gJobs); gPending=0; }
 for(Job* j:run){ if(!TryJob(j)) j->out="err сбой при выполнении (команда отменена)"; SetEvent(j->done); }
}
static void SafeTick(){ __try{ Tick(); }__except(EXCEPTION_EXECUTE_HANDLER){ gLastTick="сбой в такте"; } }
static std::atomic<ULONGLONG> gLastFrame{0}; static std::atomic<int> gBusy{0}; static std::atomic<long long> gFrames{0};
static void Frame(void* ctx){
 ULONGLONG now=GetTickCount64(); if(now-gLastFrame.load()<12) return;
 int z=0; if(!gBusy.compare_exchange_strong(z,1)) return;
 gLastFrame=now; gFrames++; gGameThread=GetCurrentThreadId();
 tCtx=ctx; if(gPending.load(std::memory_order_relaxed)) RunJobs(); SafeTick(); tCtx=nullptr; gBusy=0;
}
using HookFn=void(*)(void*,void*,void*,void*);
static HookFn gOrig=nullptr;
static void Detour(void* a,void* b,void* c,void* d){ Frame(a); gOrig(a,b,c,d); }

static int InsnLen(const uint8_t* p){
 int i=0; bool rexw=false; bool opsz=false;
 while(p[i]==0x66||p[i]==0xF2||p[i]==0xF3){ if(p[i]==0x66) opsz=true; i++; }
 if((p[i]&0xF0)==0x40){ rexw=(p[i]&8)!=0; i++; }
 uint8_t op=p[i++]; int imm=0; bool modrm=false;
 if(op>=0x50 && op<=0x5F) return i;
 if(op==0x90) return i;
 if(op==0x0F){ uint8_t o2=p[i++]; if(o2==0x10||o2==0x11||o2==0x28||o2==0x29||o2==0x57||o2==0x1F) modrm=true; else return -1; }
 else if(op==0x88||op==0x89||op==0x8A||op==0x8B||op==0x8D||op==0x01||op==0x03||op==0x29||op==0x2B||op==0x31||op==0x33||op==0x39||op==0x3B||op==0x85||op==0x84||op==0x09||op==0x0B||op==0x21||op==0x23) modrm=true;
 else if(op==0x80||op==0x83||op==0xC6){ modrm=true; imm=1; }
 else if(op==0x81||op==0xC7){ modrm=true; imm=opsz?2:4; }
 else if(op>=0xB8 && op<=0xBF) return i+(rexw?8:4);
 else if(op==0x6A) return i+1; else if(op==0x68) return i+4;
 else return -1;
 if(modrm){ uint8_t m=p[i++]; int mod=m>>6, rm=m&7;
  if(mod!=3 && rm==4){ uint8_t sib=p[i++]; if(mod==0 && (sib&7)==5) i+=4; }
  if(mod==0 && rm==5) return -1;
  if(mod==1) i+=1; else if(mod==2) i+=4; }
 return i+imm;
}

static bool HookAt(uintptr_t fn){
 uint8_t* f=(uint8_t*)fn;
 if(f[0]==0xFF && f[1]==0x25 && R<int32_t>(fn+2)==0){
  uintptr_t gate=R<uintptr_t>(fn+6), blk=gate-8; if(!Ok((void*)blk,0x40) || R<uint64_t>(blk)!=0x31425254414D4750ull) return false;
  gOrig=(HookFn)(blk+0x20); InterlockedExchange64((volatile LONG64*)(gate+6),(LONG64)(uintptr_t)&Detour); return true;
 }
 int n=0; while(n<14){ int l=InsnLen(f+n); if(l<=0) return false; n+=l; }
 uint8_t* blk=(uint8_t*)VirtualAlloc(nullptr,128,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE); if(!blk) return false;
 *(uint64_t*)blk=0x31425254414D4750ull;
 uint8_t* gate=blk+8; gate[0]=0xFF; gate[1]=0x25; memset(gate+2,0,4); uintptr_t d=(uintptr_t)&Detour; memcpy(gate+6,&d,8);
 uint8_t* tr=blk+0x20; memcpy(tr,f,n); tr[n]=0xFF; tr[n+1]=0x25; memset(tr+n+2,0,4); uintptr_t back=fn+n; memcpy(tr+n+6,&back,8);
 gOrig=(HookFn)tr;
 uint8_t patch[32]; patch[0]=0xFF; patch[1]=0x25; memset(patch+2,0,4); uintptr_t g=(uintptr_t)gate; memcpy(patch+6,&g,8); memset(patch+14,0x90,n-14);
 DWORD old; if(!VirtualProtect(f,n,PAGE_EXECUTE_READWRITE,&old)) return false; memcpy(f,patch,n); VirtualProtect(f,n,old,&old); FlushInstructionCache(GetCurrentProcess(),f,n);
 return true;
}
static bool InstallHook(){
 const char* cands[][2]={{"app.CharacterManager","onUpdate"},{"app.TimeScaleManager","updateOnFrameHead"},{"app.HackingManager","update"},{"app.WeaponManager","update"},{"app.EnhanceManager","update"}};
 for(auto& c:cands){ tdb::Method m=tdb::FindMethod(c[0],c[1],0); if(!m.fn) continue; if(HookAt(m.fn)){ gHookInfo=std::string(c[0])+"."+c[1]; return true; } }
 return false;
}

static DWORD WINAPI OwnTicker(LPVOID){ for(;;){ Sleep(16); Frame(nullptr); } }

static std::string RunOnGame(const std::string& s){
 if(GetTickCount64()-gLastFrame.load()>1500){
  int z=0; if(gBusy.compare_exchange_strong(z,1)){ Job j{s,"",nullptr}; if(!TryJob(&j)) j.out="err сбой при выполнении"; gBusy=0; return j.out; }
 }
 Job j{s,"",CreateEventW(nullptr,TRUE,FALSE,nullptr)}; { std::lock_guard<std::mutex> g(gLock); gJobs.push_back(&j); gPending=1; }
 if(WaitForSingleObject(j.done,8000)!=WAIT_OBJECT_0){
  { std::lock_guard<std::mutex> g(gLock); for(auto it=gJobs.begin();it!=gJobs.end();++it) if(*it==&j){ gJobs.erase(it); CloseHandle(j.done); return "err игра не ответила (пауза, меню или загрузка)"; } }
  WaitForSingleObject(j.done,INFINITE); }
 CloseHandle(j.done); return j.out;
}

static DWORD WINAPI ClientThread(LPVOID arg){
 HANDLE h=(HANDLE)arg; bool owner=false; std::vector<char> buf(65536);
 for(;;){ DWORD n=0; if(!ReadFile(h,buf.data(),(DWORD)buf.size()-1,&n,nullptr)||n==0) break; std::string req(buf.data(),n); std::string resp;
  if(req.rfind("set ",0)==0 || req.rfind("mul ",0)==0) owner=true;
  if(req=="ping") resp= gReady? "ok" : "err "+tdb::status;
  else if(req=="diag") resp="ok tdb="+tdb::status+" hook="+gHookInfo+" frames="+std::to_string(gFrames.load())+" vm="+Hex(tdb::vm)+" enc="+Hex(tdb::encBase);
  else if(!gReady) resp="err мост не готов: "+tdb::status;
  else resp=RunOnGame(req);
  DWORD w; if(!WriteFile(h,resp.data(),(DWORD)resp.size(),&w,nullptr)) break; }
 if(owner) for(auto k:kKeys) if(gOn[k]){ std::string r=gReady? RunOnGame(std::string("set ")+k+" 0") : ""; (void)r; }
 DisconnectNamedPipe(h); CloseHandle(h); return 0;
}
static DWORD WINAPI PipeThread(LPVOID){
 for(;;){
  HANDLE h=CreateNamedPipeA(kPipe,PIPE_ACCESS_DUPLEX,PIPE_TYPE_MESSAGE|PIPE_READMODE_MESSAGE|PIPE_WAIT,PIPE_UNLIMITED_INSTANCES,1<<20,1<<20,0,nullptr);
  if(h==INVALID_HANDLE_VALUE){ Sleep(1000); continue; }
  if(ConnectNamedPipe(h,nullptr)||GetLastError()==ERROR_PIPE_CONNECTED) CloseHandle(CreateThread(nullptr,0,ClientThread,h,0,nullptr));
  else CloseHandle(h);
 }
}

static DWORD WINAPI Init(LPVOID){
 gBase=(uintptr_t)GetModuleHandleW(nullptr);
 auto dos=(IMAGE_DOS_HEADER*)gBase; auto nt=(IMAGE_NT_HEADERS64*)(gBase+dos->e_lfanew); gSize=nt->OptionalHeader.SizeOfImage;
 CloseHandle(CreateThread(nullptr,0,PipeThread,nullptr,0,nullptr));
 for(int i=0;i<900 && !gReady;i++){ if(tdb::Init()){ tdb::BuildNames(); gReady=true; break; } Sleep(1000); }
 if(!gReady) return 0;
 if(!InstallHook()){ gHookInfo="собственный поток"; CloseHandle(CreateThread(nullptr,0,OwnTicker,nullptr,0,nullptr)); }
 return 0;
}

BOOL APIENTRY DllMain(HMODULE m,DWORD r,LPVOID){ if(r==DLL_PROCESS_ATTACH){ DisableThreadLibraryCalls(m); CloseHandle(CreateThread(nullptr,0,Init,nullptr,0,nullptr)); } return TRUE; }
