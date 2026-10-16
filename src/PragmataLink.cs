using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

sealed class PragmataLink : IDisposable {
 public const uint ExpectedStamp=0x69B75D9D, ExpectedImage=0x17924000;
 public const string ProcessName="PRAGMATA", PipeName="PragmataTrainerBridge", BridgeName="PragmataBridge.dll";
 readonly Process process;NamedPipeClientStream pipe;readonly object gate=new object();
 public readonly int Pid;
 public bool Alive{get{try{return !process.HasExited;}catch{return false;}}}

 PragmataLink(Process p){process=p;Pid=p.Id;}

 public static PragmataLink Attach(out string state){
  state="Игра не запущена";
  Process[] all=Process.GetProcessesByName(ProcessName);
  if(all.Length==0)return null;
  if(all.Length>1){state="Запущено несколько копий игры — оставьте одну";foreach(var x in all)x.Dispose();return null;}
  Process p=all[0];IntPtr h=OpenProcess(0x1F0FFF,false,p.Id);
  if(h==IntPtr.Zero){state="Нет доступа к процессу игры (запустите трейнер от имени администратора)";p.Dispose();return null;}
  try{
   ulong exe=FindModule(h,ProcessName+".exe");
   if(exe==0){state="Игра загружается…";p.Dispose();return null;}
   uint lfanew=BitConverter.ToUInt32(Read(h,exe+0x3C,4),0);byte[] nt=Read(h,exe+lfanew,0x58);
   uint stamp=BitConverter.ToUInt32(nt,8),image=BitConverter.ToUInt32(nt,0x50);
   if(stamp!=ExpectedStamp || image!=ExpectedImage){state="Другая версия игры ("+stamp.ToString("X8")+") — трейнер рассчитан на сборку PRAGMATA от 16.03.2026";p.Dispose();return null;}
   if(!HasWindow(p.Id)){state="Игра загружается…";p.Dispose();return null;}
   if(FindModule(h,BridgeName)==0){
    string dll=ExtractBridge();
    if(!Inject(h,dll)){state="Не удалось загрузить мост в игру";p.Dispose();return null;}
   }
   var link=new PragmataLink(p);
   try{link.Connect(20000);}catch(Exception e){state="Мост не отвечает: "+e.Message;link.Dispose();return null;}
   string hello=link.Send("ping");
   if(!hello.StartsWith("ok")){state="Мост готовится: "+(hello.StartsWith("err ")?hello.Substring(4):hello);link.Dispose();return null;}
   state="ok";return link;
  }catch(Exception e){state="Ошибка подключения: "+e.Message;p.Dispose();return null;}
  finally{CloseHandle(h);}
 }

 static bool HasWindow(int pid){bool found=false;EnumWindows(delegate(IntPtr w,IntPtr l){uint p;GetWindowThreadProcessId(w,out p);if(p==pid && IsWindowVisible(w)){found=true;return false;}return true;},IntPtr.Zero);return found;}

 static string ExtractBridge(){
  string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PragmataTrainer");Directory.CreateDirectory(dir);
  string path=Path.Combine(dir,BridgeName);byte[] bytes;
  using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(BridgeName)){bytes=new byte[s.Length];s.Read(bytes,0,bytes.Length);}
  try{if(!File.Exists(path) || !Same(File.ReadAllBytes(path),bytes))File.WriteAllBytes(path,bytes);}catch(IOException){}
  return path;
 }
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}

 static bool Inject(IntPtr h,string dll){
  byte[] path=Encoding.Unicode.GetBytes(dll+"\0");
  IntPtr mem=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)path.Length,0x3000,0x04);if(mem==IntPtr.Zero)return false;
  try{
   UIntPtr n;if(!WriteProcessMemory(h,mem,path,(UIntPtr)path.Length,out n))return false;
   IntPtr load=GetProcAddress(GetModuleHandle("kernel32.dll"),"LoadLibraryW");uint tid;
   IntPtr t=CreateRemoteThread(h,IntPtr.Zero,UIntPtr.Zero,load,mem,0,out tid);if(t==IntPtr.Zero)return false;
   WaitForSingleObject(t,15000);uint code;GetExitCodeThread(t,out code);CloseHandle(t);return code!=0;
  }finally{VirtualFreeEx(h,mem,UIntPtr.Zero,0x8000);}
 }

 void Connect(int ms){pipe=new NamedPipeClientStream(".",PipeName,PipeDirection.InOut);pipe.Connect(ms);pipe.ReadMode=PipeTransmissionMode.Message;}

 public string Send(string command){
  lock(gate){
   if(pipe==null || !pipe.IsConnected)throw new InvalidOperationException("Мост отключён");
   byte[] req=Encoding.UTF8.GetBytes(command);pipe.Write(req,0,req.Length);pipe.Flush();
   var ms=new MemoryStream();var buf=new byte[65536];
   do{int n=pipe.Read(buf,0,buf.Length);if(n<=0)throw new IOException("Мост закрыл канал");ms.Write(buf,0,n);}while(!pipe.IsMessageComplete);
   return Encoding.UTF8.GetString(ms.ToArray());
  }
 }

 static byte[] Read(IntPtr h,ulong address,int size){var b=new byte[size];UIntPtr n;if(!ReadProcessMemory(h,(IntPtr)(long)address,b,(UIntPtr)size,out n))throw new InvalidOperationException("Чтение памяти игры не выполнено");return b;}
 static ulong FindModule(IntPtr h,string name){
  var mods=new IntPtr[4096];uint need;
  if(!EnumProcessModulesEx(h,mods,(uint)(IntPtr.Size*mods.Length),out need,3))return 0;
  int count=Math.Min(mods.Length,(int)(need/IntPtr.Size));var sb=new StringBuilder(260);
  for(int i=0;i<count;i++){sb.Length=0;if(GetModuleBaseNameW(h,mods[i],sb,260)>0 && String.Equals(sb.ToString(),name,StringComparison.OrdinalIgnoreCase))return (ulong)mods[i].ToInt64();}
  return 0;
 }

 public void Dispose(){try{if(pipe!=null)pipe.Dispose();}catch{}pipe=null;process.Dispose();}

 delegate bool EnumProc(IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc f,IntPtr l);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr w,out uint pid);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr w);
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint type,uint protect);
 [DllImport("kernel32.dll")]static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint type);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buffer,UIntPtr size,out UIntPtr written);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,[Out]byte[] buffer,UIntPtr size,out UIntPtr read);
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr CreateRemoteThread(IntPtr h,IntPtr attr,UIntPtr stack,IntPtr start,IntPtr param,uint flags,out uint tid);
 [DllImport("kernel32.dll")]static extern uint WaitForSingleObject(IntPtr h,uint ms);
 [DllImport("kernel32.dll")]static extern bool GetExitCodeThread(IntPtr h,out uint code);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)]static extern IntPtr GetProcAddress(IntPtr mod,string name);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
 [DllImport("psapi.dll",SetLastError=true)]static extern bool EnumProcessModulesEx(IntPtr h,[Out]IntPtr[] mods,uint cb,out uint needed,uint filter);
 [DllImport("psapi.dll",CharSet=CharSet.Unicode)]static extern uint GetModuleBaseNameW(IntPtr h,IntPtr mod,StringBuilder name,uint size);
}
