using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
[assembly: AssemblyTitle("Pragmata Trainer")]
[assembly: AssemblyProduct("Pragmata Trainer")]
[assembly: AssemblyVersion("1.1.0.0")]

static class L {
 public static bool En;
 static readonly Dictionary<string,string> Map=new Dictionary<string,string>{
  {"Бессмертие","God mode"},{"Флаги NoDamage и NoDie здоровья Хью, HP всегда полное","Hugh's NoDamage and NoDie health flags, HP always full"},
  {"Неуязвимость (no hit)","Invulnerable (no hit)"},{"Попадания врагов не засчитываются (флаг Invincible)","Enemy hits do not register (Invincible flag)"},
  {"Бесконечный джетпак","Infinite jetpack"},{"Шкала двигателей всегда полная и не расходуется","The thruster gauge is always full and never drains"},
  {"Высокий прыжок ×N","High jump ×N"},{"Прыжок выше обычного (множитель высоты ниже)","Jump higher than normal (height multiplier below)"},
  {"Бесконечный кислород","Infinite oxygen"},{"Кислород не тратится, удушье не снимает HP","Oxygen is not consumed, suffocation does not drain HP"},
  {"Бесконечные ремкомплекты","Infinite repair kits"},{"Запас аптечек держится на максимуме вместимости","Repair kit stock is kept at full capacity"},
  {"Невидимость для врагов","Invisible to enemies"},{"Враги не замечают героя и теряют цель","Enemies do not notice Hugh and lose their target"},
  {"Скорость героя ×N","Hero speed ×N"},{"Ускорение времени героя (множитель ниже)","Speeds up Hugh's time (multiplier below)"},
  {"Бесконечные патроны","Infinite ammo"},{"Патроны всего оружия держатся на максимуме","Ammo of every weapon is kept at maximum"},
  {"Без перезарядки","No reload"},{"Перезарядка и перегрев завершаются мгновенно, патроны полные","Reload and overheat finish instantly, ammo stays full"},
  {"Без кулдауна оружия","No weapon cooldown"},{"Откат спецоружия завершается сразу","Special weapon cooldown ends immediately"},
  {"Максимальная точность","Perfect accuracy"},{"Разброс выстрелов оружия обнулён","Weapon shot spread is zero"},
  {"Без отдачи","No recoil"},{"Камеру не подбрасывает при стрельбе","The camera does not kick up when firing"},
  {"Ваншот","One-hit kills"},{"У врагов 1 HP — любое попадание убивает","Enemies have 1 HP — any hit kills"},
  {"Мгновенный взлом","Instant hacking"},{"Головоломка взлома Дианы проходится сразу","Diana's hacking puzzle is solved immediately"},
  {"Заморозить таймер взлома","Freeze hacking timer"},{"Таймер у замков и головоломок со временем стоит на месте","Timers of locks and timed puzzles stand still"},
  {"Бесконечная шкала хакинга","Infinite hacking gauge"},{"Шкала взлома всегда полная","The hacking gauge is always full"},
  {"Хакинг без перезарядки","No hacking cooldown"},{"Откат взлома сбрасывается каждый кадр","Hacking cooldown resets every frame"},
  {"Урон хакингом ×N","Hacking damage ×N"},{"Базовый урон взлома умножается (множитель ниже)","Base hacking damage is multiplied (multiplier below)"},
  {"Бесконечные активные навыки","Infinite active skills"},{"Заряды навыков восстанавливаются каждую секунду","Skill charges refill every second"},
  {"Заморозка врагов","Freeze enemies"},{"Полёт сквозь стены (no clip)","Fly through walls (no clip)"},{"WASD — полёт по взгляду, пробел/Ctrl — вверх/вниз, Shift — быстрее","WASD flies where you look, Space/Ctrl up/down, Shift faster"},{"Время врагов остановлено","Enemy time is stopped"},
  {"Луноволокно","Lunafilament"},{"Компоненты для улучшения","Upgrade components"},{"Чистый лунум","Pure lunum"},
  {"ПЕРЕКЛЮЧАТЕЛЕЙ","TOGGLES"},{"Ctrl + F12 — отключить все читы","Ctrl + F12 — turn every cheat off"},
  {"○ Игра не подключена","○ Game not connected"},{"Запустите PRAGMATA и загрузите сохранение","Start PRAGMATA and load a save"},
  {"Отключить всё  ·  Ctrl+F12","Turn all off  ·  Ctrl+F12"},{"Полное здоровье","Full health"},{"Здоровье восстановлено","Health restored"},
  {"Скорость героя","Hero speed"},{"Урон хакингом","Hacking damage"},{"Высота прыжка","Jump height"},{"Меняются на лету","Change on the fly"},
  {"Ресурсы, чертежи и перки — через данные самой игры · результат остаётся в сохранении","Resources, blueprints and perks use the game's own data · results stay in the save"},
  {"Количество","Amount"},{"Разблокировать все чертежи","Unlock all blueprints"},{"Чертежи разблокированы","Blueprints unlocked"},
  {"Все перки на максимум","Max out all perks"},{"Перки открыты и прокачаны","Perks unlocked and maxed"},{"Журнал","Log"},{"Справка","Help"},
  {"Звук переключений","Toggle sounds"},{"Горячие клавиши","Hotkeys"},
  {"Для этого действия нужна запущенная игра","This action needs the game running"},{"Добавлено","Added"},{"Не добавлено","Not added"},
  {"Не выполнено: ","Failed: "},{"Параметр: ","Parameter: "},{"Игра закрыта · ожидание запуска","Game closed · waiting for launch"},
  {"Игра закрыта — читы будут включены снова при следующем подключении","Game closed — cheats will be re-enabled on the next connection"},
  {"Подключено к PRAGMATA · PID ","Connected to PRAGMATA · PID "},{" · мост загружен"," · bridge loaded"},{"Не включено снова: ","Not re-enabled: "},
  {"Повторно включено: ","Re-enabled: "},{"Связь: ","Link: "},{"Связь с игрой потеряна · переподключение","Lost connection to the game · reconnecting"},
  {"Игра не подключена","Game not connected"},{"включено","on"},{"выключено","off"},{"Все функции уже выключены","Every function is already off"},
  {"Все функции выключены","All functions turned off"},{"Отключение: ","Turning off: "},{"● Подключено · PID ","● Connected · PID "},
  {"Подключение…","Connecting…"},{"Активно: ","Active: "},{" из "," of "},{"герой не найден — загрузите сохранение","hero not found — load a save"},
  {"Запустите PRAGMATA. Трейнер подключится автоматически.","Start PRAGMATA. The trainer connects automatically."},{"Сеанс ","Session "},
  {"Запуск Pragmata Trainer · PID ","Pragmata Trainer started · PID "},{"Трейнер закрыт · значения игры восстановлены","Trainer closed · game values restored"},
  {"Восстанавливаю значения игры…","Restoring game values…"},{"Pragmata Trainer · журнал","Pragmata Trainer · log"},{"Pragmata Trainer · справка","Pragmata Trainer · help"},
  {"без клавиши","no hotkey"},{" (занята)"," (taken)"},{"Горячая клавиша занята: ","Hotkey taken: "},{"Ctrl + F12 занята другим приложением","Ctrl + F12 is taken by another application"},
  {"Pragmata Trainer уже запущен.","Pragmata Trainer is already running."},{"Pragmata Trainer: ошибка","Pragmata Trainer: error"},{"Язык: русский","Language: English"},
  {"Игра не запущена","Game is not running"},{"Запущено несколько копий игры — оставьте одну","Several copies of the game are running — keep one"},
  {"Игра загружается…","Game is loading…"},{"Не удалось загрузить мост в игру","Could not load the bridge into the game"},
  {"Нет доступа к процессу игры (запустите трейнер от имени администратора)","No access to the game process (run the trainer as administrator)"},
  {"Мост закрыл канал","The bridge closed the channel"},{"Мост отключён","Bridge disconnected"},{"Чтение памяти игры не выполнено","Reading game memory failed"}
 };
 static readonly string[][] Frag={
  new[]{"Другая версия игры (","Different game version ("},new[]{") — трейнер рассчитан на сборку PRAGMATA от 16.03.2026",") — the trainer targets the PRAGMATA build from 2026-03-16"},
  new[]{"Мост готовится: ","Bridge is getting ready: "},new[]{"Мост не отвечает: ","Bridge does not respond: "},new[]{"Ошибка подключения: ","Connection error: "},
  new[]{"ок включено (игрок появится после загрузки)","on (the player appears after loading)"},new[]{"включено (игрок появится после загрузки)","on (the player appears after loading)"},
  new[]{"игра не ответила (пауза, меню или загрузка)","the game did not answer (pause, menu or loading)"},new[]{"инвентарь не найден (загрузите сохранение)","inventory not found (load a save)"},
  new[]{"менеджеры не найдены (загрузите сохранение)","managers not found (load a save)"},new[]{"игрок не найден (загрузите сохранение)","player not found (load a save)"},
  new[]{"сбой при выполнении (команда отменена)","execution failed (command cancelled)"},new[]{"записи предмета нет в инвентаре","the item has no inventory entry"},
  new[]{"не найден декодер адресов методов","method address decoder not found"},new[]{"не найдена таблица статических полей","static field table not found"},
  new[]{"мост не готов: ","bridge not ready: "},new[]{"неизвестная функция","unknown function"},new[]{"EnhanceManager не найден","EnhanceManager not found"},
  new[]{"здоровье недоступно","health unavailable"},new[]{"инвентарь не найден","inventory not found"},new[]{"не найдена VM движка","engine VM not found"},
  new[]{"таблицы TDB недоступны","TDB tables unavailable"},new[]{"неожиданная версия TDB ","unexpected TDB version "},new[]{"VM ещё не создана","VM not created yet"},
  new[]{"открыто перков: ","perks unlocked: "},new[]{", прокачано: ",", maxed: "},new[]{"таймеров взлома: ","hacking timers: "},new[]{"оружие: ","weapons: "},
  new[]{"перки: ","perks: "},new[]{"врагов: ","enemies: "},new[]{"оружия: ","weapons: "},new[]{"прыжок ","jump "},new[]{"вверх","up"},new[]{"нет объектов","objects not found"},new[]{"вниз","down"},new[]{"количество","amount"},
  new[]{"значение","value"},new[]{"сбой в такте","tick failure"},new[]{"в VM нет TDB","no TDB in the VM"},new[]{"не инициализирована","not initialised"},
  new[]{"ожидание","idle"},new[]{"исключение","exception"},new[]{"запись","write"}
 };
 public static string T(string s){if(!En || s==null)return s;string e;return Map.TryGetValue(s,out e)?e:s;}
 public static string D(string s){if(!En || String.IsNullOrEmpty(s))return s;string e;if(Map.TryGetValue(s,out e))return e;foreach(var f in Frag)s=s.Replace(f[0],f[1]);return s;}
}

static class Skin {
 public static readonly Color BG=Color.FromArgb(11,14,19),Panel=Color.FromArgb(23,29,37),Text=Color.FromArgb(232,239,246),Muted=Color.FromArgb(138,152,168),
  Accent=Color.FromArgb(98,204,242),Amber=Color.FromArgb(240,200,104),Off=Color.FromArgb(42,51,63),Red=Color.FromArgb(232,104,96),Hover=Color.FromArgb(32,40,51);
 public static Label Label(string text,int size=10,Color? color=null,bool bold=false){return new Label{Text=text,ForeColor=color??Text,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),AutoSize=false,BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleLeft};}
 public static Button Button(string text,Action action,bool primary=false){var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,BackColor=primary?Accent:Panel,ForeColor=primary?BG:Text,Font=new Font("Segoe UI",9.5f,FontStyle.Bold),Height=35,Cursor=Cursors.Hand,TabStop=true};b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(140,220,248):Hover;b.Click+=delegate{action();};return b;}
 public static void Form(Form f,string title,Size size){f.Text=title;f.ClientSize=size;f.BackColor=BG;f.ForeColor=Text;f.Font=new Font("Segoe UI",10);f.StartPosition=FormStartPosition.CenterParent;f.AutoScaleMode=AutoScaleMode.Dpi;}
}

static class Sounds {
 static SoundPlayer on,off;
 public static void Init(){on=new SoundPlayer(new MemoryStream(Wave(new[]{660.0,990.0})));off=new SoundPlayer(new MemoryStream(Wave(new[]{990.0,590.0})));on.Load();off.Load();}
 public static void Play(bool enabled){try{(enabled?on:off).Play();}catch{}}
 static byte[] Wave(double[] tones){
  const int rate=44100;int seg=rate*75/1000;int n=seg*tones.Length;var pcm=new short[n];
  for(int t=0;t<tones.Length;t++)for(int i=0;i<seg;i++){double env=Math.Min(1.0,i/(rate*0.006))*Math.Min(1.0,(seg-i)/(rate*0.018));double s=Math.Sin(2*Math.PI*tones[t]*i/rate)*0.75+Math.Sin(4*Math.PI*tones[t]*i/rate)*0.12;pcm[t*seg+i]=(short)(s*env*0.32*short.MaxValue);}
  var ms=new MemoryStream();var w=new BinaryWriter(ms);w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+n*2);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(n*2);foreach(short s in pcm)w.Write(s);return ms.ToArray();
 }
}

sealed class Cheat {public string Key,Title,Description;public bool Active,Pending;}

sealed class CheatRow : Control {
 public readonly Cheat Cheat;public readonly int Index;public string Hotkey="";public bool Available;
 public CheatRow(int index,Cheat cheat){Index=index;Cheat=cheat;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);BackColor=Skin.Panel;Height=58;Margin=new Padding(4);Cursor=Cursors.Hand;TabStop=true;AccessibleName=L.T(cheat.Title);AccessibleRole=AccessibleRole.CheckButton;}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(BackColor);
  if(Cheat.Active)using(var b=new SolidBrush(Skin.Accent))g.FillRectangle(b,0,0,3,Height);
  using(var f=new Font("Segoe UI",10,FontStyle.Bold))TextRenderer.DrawText(g,L.T(Cheat.Title),f,new Rectangle(14,7,Width-95,23),Available?Skin.Text:Skin.Muted,TextFormatFlags.EndEllipsis);
  using(var f=new Font("Segoe UI",8))TextRenderer.DrawText(g,(Hotkey!=""?Hotkey+"  ·  ":"")+L.T(Cheat.Description),f,new Rectangle(14,32,Width-90,21),Skin.Muted,TextFormatFlags.EndEllipsis);
  Rectangle box=new Rectangle(Width-66,14,52,28);using(var b=new SolidBrush(Cheat.Pending?Skin.Amber:Cheat.Active?Skin.Accent:Skin.Off))g.FillRectangle(b,box);
  using(var f=new Font("Segoe UI",9,FontStyle.Bold))TextRenderer.DrawText(g,Cheat.Pending?"…":Cheat.Active?(L.En?"ON":"ВКЛ"):(L.En?"OFF":"ВЫКЛ"),f,box,Cheat.Active||Cheat.Pending?Skin.BG:Skin.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
  if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Width-4,Height-4),Skin.Accent,BackColor);
 }
 protected override void OnMouseEnter(EventArgs e){BackColor=Skin.Hover;base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){BackColor=Skin.Panel;base.OnMouseLeave(e);}
 protected override void OnClick(EventArgs e){if(Available && !Cheat.Pending)base.OnClick(e);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space || e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
}

sealed class Trainer : Form {
 static readonly string User=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PragmataTrainer");
 public static readonly Cheat[] Cheats={
  new Cheat{Key="god",Title="Бессмертие",Description="Флаги NoDamage и NoDie здоровья Хью, HP всегда полное"},
  new Cheat{Key="nohit",Title="Неуязвимость (no hit)",Description="Попадания врагов не засчитываются (флаг Invincible)"},
  new Cheat{Key="jetpack",Title="Бесконечный джетпак",Description="Шкала двигателей всегда полная и не расходуется"},
  new Cheat{Key="highjump",Title="Высокий прыжок ×N",Description="Прыжок выше обычного (множитель высоты ниже)"},
  new Cheat{Key="oxygen",Title="Бесконечный кислород",Description="Кислород не тратится, удушье не снимает HP"},
  new Cheat{Key="repair",Title="Бесконечные ремкомплекты",Description="Запас аптечек держится на максимуме вместимости"},
  new Cheat{Key="invis",Title="Невидимость для врагов",Description="Враги не замечают героя и теряют цель"},
  new Cheat{Key="speed",Title="Скорость героя ×N",Description="Ускорение времени героя (множитель ниже)"},
  new Cheat{Key="ammo",Title="Бесконечные патроны",Description="Патроны всего оружия держатся на максимуме"},
  new Cheat{Key="noreload",Title="Без перезарядки",Description="Перезарядка и перегрев завершаются мгновенно, патроны полные"},
  new Cheat{Key="nocool",Title="Без кулдауна оружия",Description="Откат спецоружия завершается сразу"},
  new Cheat{Key="accuracy",Title="Максимальная точность",Description="Разброс выстрелов оружия обнулён"},
  new Cheat{Key="norecoil",Title="Без отдачи",Description="Камеру не подбрасывает при стрельбе"},
  new Cheat{Key="onehit",Title="Ваншот",Description="У врагов 1 HP — любое попадание убивает"},
  new Cheat{Key="hack",Title="Мгновенный взлом",Description="Головоломка взлома Дианы проходится сразу"},
  new Cheat{Key="hacktimer",Title="Заморозить таймер взлома",Description="Таймер у замков и головоломок со временем стоит на месте"},
  new Cheat{Key="hackgauge",Title="Бесконечная шкала хакинга",Description="Шкала взлома всегда полная"},
  new Cheat{Key="hackcool",Title="Хакинг без перезарядки",Description="Откат взлома сбрасывается каждый кадр"},
  new Cheat{Key="hackdmg",Title="Урон хакингом ×N",Description="Базовый урон взлома умножается (множитель ниже)"},
  new Cheat{Key="skills",Title="Бесконечные активные навыки",Description="Заряды навыков восстанавливаются каждую секунду"},
  new Cheat{Key="noclip",Title="Полёт сквозь стены (no clip)",Description="WASD — полёт по взгляду, пробел/Ctrl — вверх/вниз, Shift — быстрее"},
  new Cheat{Key="freeze",Title="Заморозка врагов",Description="Время врагов остановлено"}
 };
 static readonly string[][] Currencies={new[]{"0x84C2F1F3","Луноволокно"},new[]{"0x0B30CA95","Компоненты для улучшения"},new[]{"0x7D9B1F63","Чистый лунум"}};
 public static readonly float[] SpeedSteps={1.5f,2,2.5f,3}, HackSteps={2,3,5,10}, JumpSteps={1.5f,2,3,5,10};
 public static readonly int[] MoneySteps={10,100,1000,10000,100000};
 ComboBox speedMul,hackMul,jumpMul,moneyAmount;bool building;

 readonly List<CheatRow> rows=new List<CheatRow>();readonly List<Button> actions=new List<Button>();readonly Dictionary<int,Action> hotkeys=new Dictionary<int,Action>();
 readonly System.Windows.Forms.Timer uiTimer=new System.Windows.Forms.Timer{Interval=500};readonly DateTime started=DateTime.Now;readonly List<string> events=new List<string>();
 readonly object sync=new object();PragmataLink link;string linkState="Игра не запущена",bridgeState="";volatile bool closing,busy;Thread worker;
 Label connection,metrics,lastEvent,runtime;CheckBox sound,keys;Button ruButton,enButton;TextBox historyText;Form history;

 public Trainer(){
  Skin.Form(this,"Pragmata Trainer",new Size(980,1000));MinimumSize=new Size(900,940);StartPosition=FormStartPosition.CenterScreen;
  try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
  Directory.CreateDirectory(User);bool snd=true,hk=true;LoadSettings(ref snd,ref hk);
  Build(1,1,1,2,snd,hk);
  try{string log=Path.Combine(User,"history.log");if(File.Exists(log))events.AddRange(File.ReadAllLines(log).Reverse().Take(300).Reverse());}catch{}
  Log(L.T("Запуск Pragmata Trainer · PID ")+Process.GetCurrentProcess().Id);
  uiTimer.Tick+=delegate{Sync();};uiTimer.Start();
  worker=new Thread(Loop){IsBackground=true,Name="PragmataLink"};worker.Start();
  FormClosing+=OnClosingTrainer;
 }

 void Build(int speedIndex,int hackIndex,int jumpIndex,int moneyIndex,bool soundOn,bool keysOn){
  building=true;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18,12,18,10),ColumnCount=1};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  layout.RowCount=6;foreach(int h in new[]{70,62,12*52,48,118})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(layout);
  var head=new Panel{Dock=DockStyle.Fill};
  var image=new PictureBox{Location=new Point(1,2),Size=new Size(58,58),SizeMode=PictureBoxSizeMode.Zoom};
  try{image.Image=Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("pragmata.png"));}catch{}
  head.Controls.Add(image);
  var title=Skin.Label("PRAGMATA",24,Skin.Accent,true);title.SetBounds(73,0,360,38);head.Controls.Add(title);
  var sub=Skin.Label("DELUXE EDITION  /  TRAINER  ·  "+Cheats.Length+" "+L.T("ПЕРЕКЛЮЧАТЕЛЕЙ"),9,Skin.Muted);sub.SetBounds(76,37,420,24);head.Controls.Add(sub);
  runtime=Skin.Label("",9,Skin.Muted);runtime.TextAlign=ContentAlignment.MiddleRight;runtime.SetBounds(330,5,405,24);head.Controls.Add(runtime);
  var hk=Skin.Label(L.T("Ctrl + F12 — отключить все читы"),9,Skin.Muted);hk.TextAlign=ContentAlignment.MiddleRight;hk.SetBounds(330,30,405,24);head.Controls.Add(hk);
  ruButton=LangButton("RU",!L.En,delegate{SetLanguage(false);});enButton=LangButton("EN",L.En,delegate{SetLanguage(true);});head.Controls.Add(ruButton);head.Controls.Add(enButton);
  Action place=delegate{runtime.Left=head.Width-520;hk.Left=head.Width-520;ruButton.SetBounds(head.Width-100,8,46,30);enButton.SetBounds(head.Width-50,8,46,30);};
  head.Resize+=delegate{place();};place();layout.Controls.Add(head,0,0);
  var stat=new Panel{Dock=DockStyle.Fill,BackColor=Skin.Panel,Margin=new Padding(4,0,4,6)};connection=Skin.Label(L.T("○ Игра не подключена"),11,Skin.Amber,true);connection.SetBounds(14,4,820,27);stat.Controls.Add(connection);
  metrics=Skin.Label(L.T("Запустите PRAGMATA и загрузите сохранение"),9,Skin.Muted);metrics.SetBounds(14,30,820,22);stat.Controls.Add(metrics);
  stat.Resize+=delegate{connection.Width=stat.Width-28;metrics.Width=stat.Width-28;};layout.Controls.Add(stat,0,1);
  var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=12,Margin=new Padding(0)};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
  for(int i=0;i<12;i++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,100f/12));
  for(int i=0;i<Cheats.Length;i++){var row=new CheatRow(i,Cheats[i]){Dock=DockStyle.Fill};row.Click+=delegate{Toggle(row.Cheat);};rows.Add(row);grid.Controls.Add(row,i/11,i%11);}
  var clearRow=Skin.Button(L.T("Отключить всё  ·  Ctrl+F12"),delegate{ClearAll();});clearRow.Dock=DockStyle.Fill;clearRow.Margin=new Padding(4);clearRow.BackColor=Color.FromArgb(56,30,30);clearRow.ForeColor=Skin.Red;grid.Controls.Add(clearRow,1,11);actions.Add(clearRow);
  var healRow=Skin.Button(L.T("Полное здоровье"),delegate{Run("Здоровье восстановлено","heal");});healRow.Dock=DockStyle.Fill;healRow.Margin=new Padding(4);grid.Controls.Add(healRow,0,11);actions.Add(healRow);
  layout.Controls.Add(grid,0,2);
  var prm=new Panel{Dock=DockStyle.Fill,BackColor=Skin.Panel,Margin=new Padding(4,2,4,4)};
  var sl=Skin.Label(L.T("Скорость героя"),9,Skin.Muted);sl.SetBounds(14,9,112,24);prm.Controls.Add(sl);
  speedMul=Combo(SpeedSteps.Select(x=>"×"+x.ToString(CultureInfo.InvariantCulture)),speedIndex);speedMul.SetBounds(128,8,80,26);prm.Controls.Add(speedMul);
  var hl=Skin.Label(L.T("Урон хакингом"),9,Skin.Muted);hl.SetBounds(232,9,110,24);prm.Controls.Add(hl);
  hackMul=Combo(HackSteps.Select(x=>"×"+x.ToString(CultureInfo.InvariantCulture)),hackIndex);hackMul.SetBounds(344,8,80,26);prm.Controls.Add(hackMul);
  var jl=Skin.Label(L.T("Высота прыжка"),9,Skin.Muted);jl.SetBounds(448,9,110,24);prm.Controls.Add(jl);
  jumpMul=Combo(JumpSteps.Select(x=>"×"+x.ToString(CultureInfo.InvariantCulture)),jumpIndex);jumpMul.SetBounds(560,8,80,26);prm.Controls.Add(jumpMul);
  var pn=Skin.Label(L.T("Меняются на лету"),8,Skin.Muted);pn.SetBounds(660,9,200,24);prm.Controls.Add(pn);
  jumpMul.SelectedIndexChanged+=delegate{SendParam("highjump","Высота прыжка",JumpSteps[jumpMul.SelectedIndex]);};
  speedMul.SelectedIndexChanged+=delegate{SendParam("speed","Скорость героя",SpeedSteps[speedMul.SelectedIndex]);};hackMul.SelectedIndexChanged+=delegate{SendParam("hackdmg","Урон хакингом",HackSteps[hackMul.SelectedIndex]);};
  layout.Controls.Add(prm,0,3);
  var act=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=3,Margin=new Padding(4,3,4,0)};for(int i=0;i<4;i++)act.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
  act.RowStyles.Add(new RowStyle(SizeType.Absolute,22));for(int i=0;i<2;i++)act.RowStyles.Add(new RowStyle(SizeType.Absolute,44));
  var note=Skin.Label(L.T("Ресурсы, чертежи и перки — через данные самой игры · результат остаётся в сохранении"),8,Skin.Muted);note.Dock=DockStyle.Fill;act.Controls.Add(note,0,0);act.SetColumnSpan(note,4);
  for(int i=0;i<Currencies.Length;i++){int k=i;var b=Skin.Button("+ "+L.T(Currencies[i][1]),delegate{GiveCurrency(k);},true);Place(act,b,i,1);actions.Add(b);}
  moneyAmount=Combo(MoneySteps.Select(x=>"+"+x.ToString("N0",CultureInfo.GetCultureInfo(L.En?"en-US":"ru-RU"))),moneyIndex);moneyAmount.Dock=DockStyle.Fill;moneyAmount.Margin=new Padding(0,8,6,2);act.Controls.Add(moneyAmount,3,1);
  AddAction(act,0,2,"Разблокировать все чертежи","Чертежи разблокированы","blueprints");
  AddAction(act,1,2,"Все перки на максимум","Перки открыты и прокачаны","perks");
  Place(act,Skin.Button(L.T("Журнал"),delegate{OpenHistory();}),2,2);Place(act,Skin.Button(L.T("Справка"),delegate{OpenHelp();}),3,2);
  layout.Controls.Add(act,0,4);
  var footer=new Panel{Dock=DockStyle.Fill};sound=new CheckBox{Text=L.T("Звук переключений"),Checked=soundOn,ForeColor=Skin.Muted,AutoSize=true,Location=new Point(6,5)};keys=new CheckBox{Text=L.T("Горячие клавиши"),Checked=keysOn,ForeColor=Skin.Muted,AutoSize=true,Location=new Point(190,5)};footer.Controls.Add(sound);footer.Controls.Add(keys);
  sound.CheckedChanged+=delegate{SaveSettings();};keys.CheckedChanged+=delegate{SaveSettings();UnregisterKeys();UpdateHotkeys();};
  lastEvent=Skin.Label(events.Count>0?events[events.Count-1].Substring(Math.Min(21,events[events.Count-1].Length)):"",8,Skin.Muted);lastEvent.SetBounds(6,29,830,24);footer.Controls.Add(lastEvent);footer.Resize+=delegate{lastEvent.Width=footer.Width-12;};layout.Controls.Add(footer,0,5);
  building=false;
 }
 Button LangButton(string text,bool active,Action action){var b=Skin.Button(text,action,active);b.Font=new Font("Segoe UI",9,FontStyle.Bold);return b;}
 void SetLanguage(bool en){
  if(L.En==en)return;L.En=en;
  int s=speedMul.SelectedIndex,h=hackMul.SelectedIndex,j=jumpMul.SelectedIndex,m=moneyAmount.SelectedIndex;bool snd=sound.Checked,k=keys.Checked;
  UnregisterKeys();SuspendLayout();var old=Controls.Cast<Control>().ToList();Controls.Clear();foreach(var c in old)c.Dispose();rows.Clear();actions.Clear();
  Build(s,h,j,m,snd,k);ResumeLayout(true);SaveSettings();Sync();Log(L.T("Язык: русский"));
 }
 ComboBox Combo(IEnumerable<string> items,int def){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Skin.BG,ForeColor=Skin.Text,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",10)};foreach(var s in items)c.Items.Add(s);
  c.DrawMode=DrawMode.OwnerDrawFixed;c.ItemHeight=22;c.DrawItem+=delegate(object o,DrawItemEventArgs e){bool sel=(e.State&DrawItemState.Selected)!=0 && (e.State&DrawItemState.ComboBoxEdit)==0;using(var b=new SolidBrush(sel?Color.FromArgb(36,62,80):Skin.Panel))e.Graphics.FillRectangle(b,e.Bounds);if(e.Index>=0)TextRenderer.DrawText(e.Graphics,c.Items[e.Index].ToString(),c.Font,e.Bounds,Skin.Accent,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};
  c.SelectedIndex=Math.Max(0,Math.Min(def,c.Items.Count-1));return c;}
 void GiveCurrency(int index){
  if(!Live()){Post(L.T("Для этого действия нужна запущенная игра"));return;}
  int n=MoneySteps[moneyAmount.SelectedIndex];string id=Currencies[index][0];string name=Currencies[index][1];
  Task.Run(delegate{try{string r=Send("give "+id+" "+n);bool ok=r.StartsWith("ok");Post(L.T(ok?"Добавлено":"Не добавлено")+" +"+n+": "+L.T(name)+" · "+L.D(r.Substring(ok?3:Math.Min(4,r.Length))));if(ok)BeginInvoke((Action)delegate{Beep(true);});}catch(Exception e){Post(L.T("Не выполнено: ")+L.D(e.Message));}});
 }
 void SendParam(string name,string title,float v){if(building || !Live())return;Task.Run(delegate{try{Send("mul "+name+" "+v.ToString(CultureInfo.InvariantCulture));Post(L.T(title)+": ×"+v.ToString(CultureInfo.InvariantCulture));}catch(Exception e){Post(L.T("Параметр: ")+L.D(e.Message));}});}
 void PushParams(){try{Send("mul speed "+SpeedSteps[speedMul.SelectedIndex].ToString(CultureInfo.InvariantCulture));Send("mul hackdmg "+HackSteps[hackMul.SelectedIndex].ToString(CultureInfo.InvariantCulture));Send("mul highjump "+JumpSteps[jumpMul.SelectedIndex].ToString(CultureInfo.InvariantCulture));}catch{}}
 void Place(TableLayoutPanel p,Button b,int x,int y){b.Dock=DockStyle.Fill;b.Margin=new Padding(0,2,6,2);p.Controls.Add(b,x,y);}
 void AddAction(TableLayoutPanel p,int x,int y,string title,string done,string command){var b=Skin.Button(L.T(title),delegate{Run(done,command);});Place(p,b,x,y);actions.Add(b);}
 bool Live(){PragmataLink l=link;return !closing && l!=null && l.Alive;}

 void Loop(){
  DateTime nextAttach=DateTime.MinValue,nextState=DateTime.MinValue;
  while(!closing){
   try{
    if(link!=null && !link.Alive){lock(sync){try{link.Dispose();}catch{}link=null;}linkState="Игра закрыта · ожидание запуска";Post(L.T("Игра закрыта — читы будут включены снова при следующем подключении"));}
    if(link==null && DateTime.UtcNow>=nextAttach){
     nextAttach=DateTime.UtcNow.AddSeconds(3);string state;var l=PragmataLink.Attach(out state);linkState=state;
     if(l!=null){lock(sync)link=l;linkState="ok";Post(L.T("Подключено к PRAGMATA · PID ")+l.Pid+L.T(" · мост загружен"));PushParams();
      var restore=Cheats.Where(c=>c.Active).ToList();
      foreach(var c in restore)try{Send("set "+c.Key+" 1");}catch(Exception e){c.Active=false;Post(L.T("Не включено снова: ")+L.T(c.Title)+" · "+L.D(e.Message));}
      if(restore.Count>0)Post(L.T("Повторно включено: ")+String.Join(", ",restore.Select(c=>L.T(c.Title))));}
    }
    if(link!=null && !busy && DateTime.UtcNow>=nextState){nextState=DateTime.UtcNow.AddSeconds(2);string s=Send("state");bridgeState=s.StartsWith("ok ")?s.Substring(3):s;}
   }catch(Exception e){
    Post(L.T("Связь: ")+L.D(e.Message));
    lock(sync){if(link!=null){try{link.Dispose();}catch{}link=null;}}linkState="Связь с игрой потеряна · переподключение";
   }
   Thread.Sleep(200);
  }
 }
 string Send(string command){PragmataLink l;lock(sync)l=link;if(l==null)throw new InvalidOperationException(L.T("Игра не подключена"));return l.Send(command);}

 void Toggle(Cheat c){
  if(!Live() || c.Pending || busy)return;c.Pending=true;Sync();bool target=!c.Active;
  Task.Run(delegate{
   try{string r=Send("set "+c.Key+" "+(target?1:0));if(!r.StartsWith("ok"))throw new InvalidOperationException(r.StartsWith("err ")?r.Substring(4):r);
    c.Active=target;Post(L.T(c.Title)+": "+L.T(target?"включено":"выключено")+(r.Length>3?" · "+L.D(r.Substring(3)):""));BeginInvoke((Action)delegate{Beep(target);});}
   catch(Exception e){Post(L.T("Не выполнено: ")+L.T(c.Title)+" · "+L.D(e.Message));}
   finally{c.Pending=false;}
  });
 }
 void ClearAll(){
  if(!Live() || busy)return;var on=Cheats.Where(c=>c.Active).ToList();if(on.Count==0){Post(L.T("Все функции уже выключены"));return;}busy=true;
  Task.Run(delegate{try{foreach(var c in on){c.Pending=true;try{Send("set "+c.Key+" 0");c.Active=false;}finally{c.Pending=false;}}Post(L.T("Все функции выключены")+" ("+on.Count+")");BeginInvoke((Action)delegate{Beep(false);});}catch(Exception e){Post(L.T("Отключение: ")+L.D(e.Message));}finally{busy=false;}});
 }
 void Run(string title,string command){
  if(!Live()){Post(L.T("Для этого действия нужна запущенная игра"));return;}
  Task.Run(delegate{try{string r=Send(command);if(!r.StartsWith("ok"))throw new InvalidOperationException(r.StartsWith("err ")?r.Substring(4).Split('\n')[0]:r);string extra=r.Length>3?r.Substring(3).Split('\n')[0]:"";Post(L.T(title)+(extra!=""?" · "+L.D(extra):""));BeginInvoke((Action)delegate{Beep(true);});}catch(Exception e){Post(L.T("Не выполнено: ")+L.D(e.Message));}});
 }
 [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int attr,ref int value,int size);
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);int dark=1;try{DwmSetWindowAttribute(Handle,20,ref dark,4);}catch{}}

 void Sync(){
  bool live=Live();PragmataLink l=link;
  connection.Text=live?L.T("● Подключено · PID ")+l.Pid+" · PRAGMATA Deluxe Edition":"○ "+(linkState=="ok"?L.T("Подключение…"):L.D(linkState));
  connection.ForeColor=live?Skin.Accent:Skin.Amber;
  int active=Cheats.Count(c=>c.Active);string detail="";
  if(bridgeState.Contains("|")){detail=bridgeState.Split('|')[1].Trim();}
  bool noPlayer=bridgeState.Contains("player=0x0 ");
  metrics.Text=live?L.T("Активно: ")+active+L.T(" из ")+Cheats.Length+(noPlayer?"   ·   "+L.T("герой не найден — загрузите сохранение"):detail!="" && detail!="ожидание"?"   ·   "+L.D(detail.Replace("hp=","HP ").Replace("оружие=","оружия: ")):"")
   :L.T("Запустите PRAGMATA. Трейнер подключится автоматически.");
  foreach(var r in rows){r.Available=live && !busy;r.Invalidate();}foreach(var b in actions)b.Enabled=live && !busy;
  runtime.Text=L.T("Сеанс ")+(DateTime.Now-started).ToString(@"hh\:mm\:ss")+"  ·  PID "+Process.GetCurrentProcess().Id;
  UpdateHotkeys();
 }
 void Beep(bool on){if(sound.Checked)Sounds.Play(on);}
 void Post(string message){try{if(IsHandleCreated && !IsDisposed)BeginInvoke((Action)delegate{Log(message);});}catch{}}
 void Log(string message){string line=DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")+"  "+message;events.Add(line);if(events.Count>1000)events.RemoveRange(0,events.Count-1000);if(lastEvent!=null)lastEvent.Text=message;if(historyText!=null && !historyText.IsDisposed)historyText.AppendText(line+Environment.NewLine);try{File.AppendAllText(Path.Combine(User,"history.log"),line+Environment.NewLine);}catch{}}
 void LoadSettings(ref bool snd,ref bool hk){try{string f=Path.Combine(User,"settings.txt");if(!File.Exists(f)){L.En=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName!="ru";return;}foreach(string line in File.ReadAllLines(f)){var p=line.Split('=');if(p.Length!=2)continue;if(p[0]=="sound")snd=p[1]=="1";if(p[0]=="hotkeys")hk=p[1]=="1";if(p[0]=="lang")L.En=p[1]=="en";}}catch{}}
 void SaveSettings(){if(building)return;try{File.WriteAllText(Path.Combine(User,"settings.txt"),"sound="+(sound.Checked?1:0)+"\r\nhotkeys="+(keys.Checked?1:0)+"\r\nlang="+(L.En?"en":"ru")+"\r\n");}catch{}}
 void OpenHistory(){if(history!=null && !history.IsDisposed){history.Activate();return;}history=new Form();Skin.Form(history,L.T("Pragmata Trainer · журнал"),new Size(860,500));history.Icon=Icon;historyText=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BackColor=Skin.Panel,ForeColor=Skin.Text,BorderStyle=BorderStyle.None,Font=new Font("Consolas",9.5f),Text=String.Join(Environment.NewLine,events.ToArray())+Environment.NewLine};history.Controls.Add(historyText);history.Show(this);historyText.SelectionStart=historyText.TextLength;historyText.ScrollToCaret();}
 void OpenHelp(){var f=new Form();Skin.Form(f,L.T("Pragmata Trainer · справка"),new Size(720,560));f.Icon=Icon;
  var keysText=String.Join("\r\n",rows.Select(r=>"  "+r.Hotkey.PadRight(16)+L.T(r.Cheat.Title)));
  string text=L.En?
   "Pragmata Trainer works with PRAGMATA Deluxe Edition (PRAGMATA.exe built 2026-03-16).\r\n\r\n"+
   "1. Start the game and load a save.\r\n2. Open the trainer — it connects by itself (launch order does not matter).\r\n\r\n"+
   "How it works: the trainer loads the PragmataBridge.dll bridge into the game. The bridge finds the RE Engine virtual machine and the game's type database in memory "+
   "and works with the game's own classes by name: HitPoint (NoDamage/NoDie/Invincible flags), thruster and hacking GaugeUnit, PlayerCoreData (oxygen, hacking damage), "+
   "app.Weapon (ammo, reload and cooldown timers, spread and camera recoil), puzzles (forced success and timer pause), EnemyFindManager (detection), "+
   "via.GameObject time (hero speed, enemy freeze).\r\n\r\n"+
   "Buttons: «+ Lunafilament», «+ Upgrade components» and «+ Pure lunum» add the selected amount to the game's own inventory entries. "+
   "«Unlock all blueprints» unlocks every weapon of the module printer and every perk (Hacking and Add-ons tabs). These results stay in the save.\r\n\r\n"+
   "Turning a function off restores the original values. Turn functions off before saving if you do not want changed parameters in the save file.\r\n\r\n"+
   "Hotkeys (also work in game):\r\n"+keysText+"\r\n  Ctrl+F12          Turn everything off\r\n\r\n"+
   "Closing the trainer turns every toggle off and restores the original values.\r\n"+
   "Log and settings: %LOCALAPPDATA%\\PragmataTrainer"
   :
   "Pragmata Trainer работает с PRAGMATA Deluxe Edition (PRAGMATA.exe от 16.03.2026).\r\n\r\n"+
   "1. Запустите игру и загрузите сохранение.\r\n2. Откройте трейнер — он подключится сам (порядок запуска не важен).\r\n\r\n"+
   "Как это работает: трейнер загружает в игру мост PragmataBridge.dll. Мост находит в памяти виртуальную машину RE Engine и базу типов игры "+
   "и обращается к классам самой игры по именам: HitPoint (флаги NoDamage/NoDie/Invincible), GaugeUnit двигателей и хакинга, PlayerCoreData (кислород, урон взлома), "+
   "оружие app.Weapon (патроны, таймеры перезарядки и отката, разброс и отдача камеры), головоломки (принудительный успех и пауза таймера), EnemyFindManager (обнаружение), "+
   "время объектов via.GameObject (скорость героя и заморозка врагов).\r\n\r\n"+
   "Кнопки «+ Луноволокно», «+ Компоненты для улучшения» и «+ Чистый лунум» добавляют выбранное количество в записи инвентаря самой игры. "+
   "«Разблокировать все чертежи» открывает всё оружие модульного принтера и все перки (вкладки «Взлом» и «Дополнения»). Эти результаты остаются в сохранении.\r\n\r\n"+
   "Выключение функции возвращает исходные значения. Выключайте функции перед сохранением, если не хотите, чтобы изменённые параметры попали в файл.\r\n\r\n"+
   "Горячие клавиши (работают и в игре):\r\n"+keysText+"\r\n  Ctrl+F12          Отключить всё\r\n\r\n"+
   "При закрытии трейнера все переключатели выключаются, исходные значения возвращаются.\r\n"+
   "Журнал и настройки: %LOCALAPPDATA%\\PragmataTrainer";
  var info=new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,BackColor=Skin.Panel,ForeColor=Skin.Text,BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.Vertical,Font=new Font("Segoe UI",10),Text=text};
  f.Controls.Add(info);f.Show(this);}

 [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
 void UpdateHotkeys(){
  bool enable=Live() && keys.Checked;if(!enable){UnregisterKeys();foreach(var r in rows)r.Hotkey=HotkeyName(r.Index);return;}
  if(hotkeys.Count>0 || !IsHandleCreated)return;
  foreach(var row in rows){int i=row.Index;row.Hotkey=HotkeyName(i);int id=100+i;
   if(row.Cheat.Key=="noclip"){if(RegisterHotKey(Handle,id,0x4002,0x6E)){CheatRow cap=row;hotkeys[id]=delegate{Toggle(cap.Cheat);};}continue;}
   if(i>=20)continue;
   int j=i%10;uint vk=(uint)(j<9?0x61+j:0x60);uint mods=(uint)(0x4000|2|(i>=10?1:0));
   if(RegisterHotKey(Handle,id,mods,vk)){CheatRow capture=row;hotkeys[id]=delegate{Toggle(capture.Cheat);};}else{row.Hotkey+=L.T(" (занята)");Log(L.T("Горячая клавиша занята: ")+L.T(row.Cheat.Title));}}
  if(RegisterHotKey(Handle,200,0x4002,0x7B))hotkeys[200]=delegate{ClearAll();};else Log(L.T("Ctrl + F12 занята другим приложением"));
 }
 static string HotkeyName(int i){if(i<Cheats.Length && Cheats[i].Key=="noclip")return "Ctrl+Num.";if(i>=20)return L.T("без клавиши");int j=i%10;return (i<10?"Ctrl":"Ctrl+Alt")+"+Num"+(j<9?j+1:0);}
 void UnregisterKeys(){if(!IsHandleCreated)return;foreach(int id in hotkeys.Keys)UnregisterHotKey(Handle,id);hotkeys.Clear();}
 protected override void WndProc(ref Message m){if(m.Msg==0x312){Action a;if(hotkeys.TryGetValue(m.WParam.ToInt32(),out a))a();}base.WndProc(ref m);}

 bool shutdownDone;
 void OnClosingTrainer(object sender,FormClosingEventArgs e){
  if(shutdownDone)return;e.Cancel=true;if(closing)return;closing=true;uiTimer.Stop();UnregisterKeys();
  foreach(var r in rows)r.Available=false;foreach(var b in actions)b.Enabled=false;connection.Text=L.T("Восстанавливаю значения игры…");
  Task.Run(delegate{
   try{PragmataLink l;lock(sync){l=link;link=null;}if(l!=null){if(l.Alive)foreach(var c in Cheats.Where(c=>c.Active))try{l.Send("set "+c.Key+" 0");}catch{}l.Dispose();}}catch{}
   BeginInvoke((Action)delegate{Log(L.T("Трейнер закрыт · значения игры восстановлены"));shutdownDone=true;Close();});
  });
 }

 static int SelfTest(string[] args){
  int i=Array.IndexOf(args,"--selftest");string report=i+1<args.Length?args[i+1]:"selftest.txt";var log=new StringBuilder();
  Action<string> say=s=>{log.AppendLine(DateTime.Now.ToString("HH:mm:ss.fff ")+s);File.WriteAllText(report,log.ToString(),Encoding.UTF8);};
  string state;var link=PragmataLink.Attach(out state);say("attach: "+state);if(link==null)return 2;
  try{for(int k=i+2;k<args.Length;k++){string a=args[k];if(a.StartsWith("wait ")){Thread.Sleep(int.Parse(a.Substring(5)));say(a);continue;}say("> "+a);say(link.Send(a));}return 0;}
  catch(Exception e){say("error: "+e);return 1;}finally{link.Dispose();}
 }

 [STAThread]static void Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  if(args.Contains("--selftest")){Environment.ExitCode=SelfTest(args);return;}
  int li=Array.IndexOf(args,"--lang");
  bool first;using(var mutex=new Mutex(true,"Local\\PragmataTrainer.v1",out first)){
   if(!first){MessageBox.Show(L.T("Pragmata Trainer уже запущен."),"Pragmata Trainer");return;}
   try{Sounds.Init();var form=new Trainer();if(li>=0 && li+1<args.Length)form.SetLanguage(args[li+1]=="en");int si=Array.IndexOf(args,"--shot");
    if(si>=0 && si+1<args.Length){string shot=Path.GetFullPath(args[si+1]);var t=new System.Windows.Forms.Timer{Interval=6000};
     t.Tick+=delegate{t.Stop();using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(0,0,form.Width,form.Height));b.Save(shot);}form.Close();};
     form.Shown+=delegate{t.Start();};}
    Application.Run(form);}catch(Exception e){MessageBox.Show(e.Message,L.T("Pragmata Trainer: ошибка"));Environment.ExitCode=1;}
  }
 }
}
