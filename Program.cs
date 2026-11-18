using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows.Forms;
using System.Drawing;
static class Program {
[STAThread] static void Main(string[] args) {
ApplicationConfiguration.Initialize();
try { if(args.Contains("--smoke-test")) { using var f=new Editor(); return; } Application.Run(new Editor()); } catch(Exception e){MessageBox.Show(e.ToString(),"Startfehler");}
}}
public class Layer {
public string Text {get;set;}="";
public float X {get;set;}=50;
public float Y {get;set;}=50;
public float Size {get;set;}=48;
public string FontName {get;set;}="Arial";
public int Color {get;set;}=ColorWhite;
const int ColorWhite=-1;
public string Kind {get;set;}="Text";
public float W {get;set;}=400;
public float H {get;set;}=100;
public string ImageData {get;set;}="";
public bool Visible {get;set;}=true;
public override string ToString()=>Kind+": "+Text;
}
public class Editor:Form {
List<Layer> layers=new(); ListBox list=new(){Dock=DockStyle.Top,Height=180}; PropertyGrid props=new(){Dock=DockStyle.Fill}; Panel preview=new(){Dock=DockStyle.Fill,BackColor=Color.Gray}; Layer? drag; Point last; float scale=1; PointF offset;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"OMSI-SEV","Vorlagen");
public Editor(){
Text="OMSI SEV Generator 3.2"; Width=1250;Height=800; Directory.CreateDirectory(dir);
var side=new Panel{Dock=DockStyle.Left,Width=330}; var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=160,AutoScroll=true}; side.Controls.Add(props);side.Controls.Add(list);side.Controls.Add(tools);Controls.Add(preview);Controls.Add(side);
void Btn(string t,Action a){var b=new Button{Text=t,AutoSize=true};b.Click+=(s,e)=>{try{a();}catch(Exception ex){MessageBox.Show(ex.Message);}};tools.Controls.Add(b);}
Btn("Text",()=>Add(new(){Text="Haltestelle"}));Btn("Farbfläche",()=>Add(new(){Kind="Fläche",Color=Color.Navy.ToArgb()}));Btn("Logo",()=>{using var d=new OpenFileDialog{Filter="Bilder|*.png;*.jpg;*.bmp"};if(d.ShowDialog()==DialogResult.OK)Add(new(){Kind="Logo",Text=Path.GetFileName(d.FileName),ImageData=Convert.ToBase64String(File.ReadAllBytes(d.FileName)),W=180,H=100});});
Btn("Farbe",()=>{if(list.SelectedItem is Layer l){using var d=new ColorDialog{Color=Color.FromArgb(l.Color)};if(d.ShowDialog()==DialogResult.OK){l.Color=d.Color.ToArgb();props.Refresh();preview.Invalidate();}}});
Btn("Schrift",()=>{if(list.SelectedItem is Layer l){using var d=new FontDialog();if(d.ShowDialog()==DialogResult.OK){l.FontName=d.Font.Name;l.Size=d.Font.Size;props.Refresh();preview.Invalidate();}}});
Btn("Löschen",()=>{if(list.SelectedItem is Layer l){layers.Remove(l);RefreshList();}});Btn("Nach vorne",()=>{if(list.SelectedItem is Layer l){layers.Remove(l);layers.Add(l);RefreshList();list.SelectedItem=l;}});
Btn("Speichern",()=>{using var d=new SaveFileDialog{InitialDirectory=dir,Filter="SEV Vorlage|*.json"};if(d.ShowDialog()==DialogResult.OK)File.WriteAllText(d.FileName,JsonSerializer.Serialize(layers));});
Btn("Öffnen",()=>{using var d=new OpenFileDialog{InitialDirectory=dir,Filter="SEV Vorlage|*.json"};if(d.ShowDialog()==DialogResult.OK){layers=JsonSerializer.Deserialize<List<Layer>>(File.ReadAllText(d.FileName))??new();RefreshList();}});
Btn("Export",()=>{using var d=new SaveFileDialog{Filter="PNG|*.png|BMP|*.bmp"};if(d.ShowDialog()==DialogResult.OK){using var b=new Bitmap(1200,800);using var g=Graphics.FromImage(b);Draw(g);b.Save(d.FileName,d.FilterIndex==2?System.Drawing.Imaging.ImageFormat.Bmp:System.Drawing.Imaging.ImageFormat.Png);}});
Btn("Updates",()=>CheckUpdate(true)); 
list.SelectedIndexChanged+=(s,e)=>props.SelectedObject=list.SelectedItem;props.PropertyValueChanged+=(s,e)=>preview.Invalidate();
preview.Paint+=(s,e)=>{scale=Math.Min(preview.Width/1200f,preview.Height/800f);offset=new((preview.Width-1200*scale)/2,(preview.Height-800*scale)/2);e.Graphics.TranslateTransform(offset.X,offset.Y);e.Graphics.ScaleTransform(scale,scale);e.Graphics.SetClip(new Rectangle(0,0,1200,800));Draw(e.Graphics);};
preview.MouseDown+=(s,e)=>{float x=(e.X-offset.X)/scale,y=(e.Y-offset.Y)/scale;drag=layers.LastOrDefault(l=>l.Visible&&x>=l.X&&x<=l.X+l.W&&y>=l.Y&&y<=l.Y+l.H);if(drag!=null){list.SelectedItem=drag;last=e.Location;preview.Capture=true;}};
preview.MouseMove+=(s,e)=>{if(drag!=null){drag.X+=(e.X-last.X)/scale;drag.Y+=(e.Y-last.Y)/scale;last=e.Location;preview.Invalidate();props.Refresh();}};preview.MouseUp+=(s,e)=>{drag=null;preview.Capture=false;};preview.Resize+=(s,e)=>preview.Invalidate();
layers.Add(new(){Kind="Fläche",Text="Hintergrund",X=0,Y=0,W=1200,H=800,Color=Color.Navy.ToArgb()});layers.Add(new(){Text="Schienenersatzverkehr",X=40,Y=35,Size=65,W=1120,H=100});layers.Add(new(){Text="Startbahnhof S",X=80,Y=230,W=1040});layers.Add(new(){Text="Zwischenhalt",X=80,Y=370,W=1040});layers.Add(new(){Text="Zielbahnhof",X=80,Y=510,W=1040});RefreshList();Shown+=(s,e)=>CheckUpdate(false);
}
void Add(Layer l){layers.Add(l);RefreshList();list.SelectedItem=l;}
void RefreshList(){list.Items.Clear();foreach(var l in layers)list.Items.Add(l);preview.Invalidate();}
void Draw(Graphics g){g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;foreach(var l in layers.Where(l=>l.Visible)){using var brush=new SolidBrush(Color.FromArgb(l.Color));if(l.Kind=="Fläche")g.FillRectangle(brush,l.X,l.Y,l.W,l.H);else if(l.Kind=="Logo"){using var ms=new MemoryStream(Convert.FromBase64String(l.ImageData));using var im=Image.FromStream(ms);g.DrawImage(im,l.X,l.Y,l.W,l.H);}else{using var f=new Font(l.FontName,Math.Max(1,l.Size),GraphicsUnit.Pixel);bool sb=l.Text.EndsWith(" S",StringComparison.OrdinalIgnoreCase);string t=sb?l.Text[..^2]:l.Text;g.DrawString(t,f,brush,l.X,l.Y);if(sb){float x=l.X+g.MeasureString(t,f).Width;using var green=new SolidBrush(Color.SeaGreen);g.FillEllipse(green,x,l.Y,l.Size,l.Size);g.DrawString("S",f,Brushes.White,x,l.Y);}}}}
async void CheckUpdate(bool manual){try{using var h=new HttpClient();h.DefaultRequestHeaders.UserAgent.ParseAdd("OMSI-SEV/3.2");var s=await h.GetStringAsync("https://api.github.com/repos/danielmeller2016-ai/OMSI-SEV-Generator/releases/latest");using var j=JsonDocument.Parse(s);var tag=j.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v');if(Version.TryParse(tag,out var v)&&v>new Version(3,2,0)){if(MessageBox.Show("Neue Version "+v+" verfügbar. Download öffnen?","Update",MessageBoxButtons.YesNo)==DialogResult.Yes)Process.Start(new ProcessStartInfo("https://github.com/danielmeller2016-ai/OMSI-SEV-Generator/releases/latest"){UseShellExecute=true});}else if(manual)MessageBox.Show("Aktuell");}catch(Exception e){if(manual)MessageBox.Show("Updateprüfung: "+e.Message);}}
}