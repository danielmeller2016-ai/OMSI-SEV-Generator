using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Security.Cryptography;
static class AutoUpdate {
 const string Repo="danielmeller2016-ai/OMSI-SEV-Generator";
 public static bool Apply(string[] args) {
 if(args.Length!=4 || args[0]!="--apply-update") return false;
 string target=args[1], staged=args[2], backup=target+".previous";
 try {
 if(int.TryParse(args[3],out int pid)) {try {using var old=Process.GetProcessById(pid);if(!old.WaitForExit(30000))throw new IOException("Programm noch geöffnet");}catch(ArgumentException){}}
 File.Copy(target,backup,true);
 try {File.Copy(staged,target,true);Process.Start(new ProcessStartInfo(target){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(target)!});}
 catch {File.Copy(backup,target,true);throw;}
 }catch(Exception e){MessageBox.Show(e.Message,"Update fehlgeschlagen");}
 return true;
 }
 public static async Task<bool> CheckAsync(){
 try {
 using var http=new HttpClient{Timeout=TimeSpan.FromMinutes(5)};
 http.DefaultRequestHeaders.UserAgent.ParseAdd("OMSI-SEV-Updater/3.4");
 using var json=JsonDocument.Parse(await http.GetStringAsync("https://api.github.com/repos/"+Repo+"/releases/latest"));
 var root=json.RootElement;
 if(!Version.TryParse(root.GetProperty("tag_name").GetString()?.TrimStart('v'),out var latest)||latest<=new Version(3,4,0))return false;
 var assets=root.GetProperty("assets").EnumerateArray().ToArray();
 var exe=assets.FirstOrDefault(a=>a.GetProperty("name").GetString()=="OMSI-SEV-Generator.exe");
 if(exe.ValueKind==JsonValueKind.Undefined)return false;
 if(!exe.TryGetProperty("digest",out var digest)||digest.GetString() is not string expected||!expected.StartsWith("sha256:"))return false;
 var folder=Path.Combine(Path.GetTempPath(),"OMSI-SEV-Update",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
 var staged=Path.Combine(folder,"new.exe");var helper=Path.Combine(folder,"updater.exe");
 using(var stream=await http.GetStreamAsync(exe.GetProperty("browser_download_url").GetString()!)){await using var output=File.Create(staged);await stream.CopyToAsync(output);}
 using(var input=File.OpenRead(staged)){if(!Convert.ToHexString(SHA256.HashData(input)).Equals(expected[7..],StringComparison.OrdinalIgnoreCase))throw new IOException("Prüfsumme ungültig");}
 string target=Environment.ProcessPath!;File.Copy(target,helper);
 var start=new ProcessStartInfo(helper){UseShellExecute=false};start.ArgumentList.Add("--apply-update");start.ArgumentList.Add(target);start.ArgumentList.Add(staged);start.ArgumentList.Add(Environment.ProcessId.ToString());
 Process.Start(start);return true;
 }catch(Exception e){try{File.AppendAllText(Path.Combine(Path.GetTempPath(),"OMSI-SEV-update.log"),DateTime.Now+" "+e+"\n");}catch{}return false;}
 }
}