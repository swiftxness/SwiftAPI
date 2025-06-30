using coms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SwiftAPI;

public class VelAPI
{
  private HttpClient client = new HttpClient();
  private string current_injector_url = "https://gitlab.com/goldben986/velocity/-/raw/main/erto3e4rortoergn.exe";
  private string currret_decompiler_url = "https://gitlab.com/goldben986/velocity/-/raw/main/Decompiler.exe";
  private string current_version_url = "https://gitlab.com/goldben986/velocity/-/raw/main/current_version.txt";
  private Process decompilerProcess;
  public VelocityStates VelocityStatus = VelocityStates.NotAttached;
  public List<int> injected_pids = new List<int>();
  private Timer CommunicationTimer;

  public static string Base64Encode(string plainText)
  {
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
  }

  public static byte[] Base64Decode(string plainText) => Convert.FromBase64String(plainText);

  private bool IsPidRunning(int pid)
  {
    try
    {
      Process.GetProcessById(pid);
      return true;
    }
    catch (ArgumentException ex)
    {
      return false;
    }
  }

  private async Task AutoUpdateAsync()
  {
    string remoteVersion;
    try
    {
      remoteVersion = await this.client.GetStringAsync(this.current_version_url);
    }
    catch (Exception)
    {
      return;
    }

    string versionPath = Path.Combine("Bin", "current_version.txt");
    string localVersion = File.Exists(versionPath) ? File.ReadAllText(versionPath) : string.Empty;

    if (remoteVersion != localVersion)
    {
      string injectorPath = Path.Combine("Bin", "erto3e4rortoergn.exe");
      string decompilerPath = Path.Combine("Bin", "Decompiler.exe");

      if (File.Exists(injectorPath))
        File.Delete(injectorPath);
      if (File.Exists(decompilerPath))
        File.Delete(decompilerPath);

      HttpResponseMessage injectorResp = await this.client.GetAsync(this.current_injector_url);
      if (injectorResp.IsSuccessStatusCode)
        File.WriteAllBytes(injectorPath, await injectorResp.Content.ReadAsByteArrayAsync());

      HttpResponseMessage decompilerResp = await this.client.GetAsync(this.currret_decompiler_url);
      if (decompilerResp.IsSuccessStatusCode)
        File.WriteAllBytes(decompilerPath, await decompilerResp.Content.ReadAsByteArrayAsync());
    }

    File.WriteAllText(versionPath, remoteVersion);
  }

  public async Task StartCommunication()
  {
    if (!Directory.Exists("Bin"))
      Directory.CreateDirectory("Bin");
    if (!Directory.Exists("AutoExec"))
      Directory.CreateDirectory("AutoExec");
    if (!Directory.Exists("Workspace"))
      Directory.CreateDirectory("Workspace");
    if (!Directory.Exists("Scripts"))
      Directory.CreateDirectory("Scripts");
    await this.AutoUpdateAsync();
    this.StopCommunication();
    this.decompilerProcess = new Process();
    this.decompilerProcess.StartInfo.FileName = "Bin\\Decompiler.exe";
    this.decompilerProcess.StartInfo.UseShellExecute = false;
    this.decompilerProcess.EnableRaisingEvents = true;
    this.decompilerProcess.StartInfo.RedirectStandardError = true;
    this.decompilerProcess.StartInfo.RedirectStandardInput = true;
    this.decompilerProcess.StartInfo.RedirectStandardOutput = true;
    this.decompilerProcess.StartInfo.CreateNoWindow = true;
    this.decompilerProcess.Start();
    this.CommunicationTimer = new Timer(100.0);
    this.CommunicationTimer.Elapsed += (ElapsedEventHandler) ((source, e) =>
    {
      foreach (int injectedPid in this.injected_pids)
      {
        if (!this.IsPidRunning(injectedPid))
          this.injected_pids.Remove(injectedPid);
      }
      string plainText = $"setworkspacefolder: {Directory.GetCurrentDirectory()}\\Workspace";
      foreach (int injectedPid in this.injected_pids)
        NamedPipes.LuaPipe(VelAPI.Base64Encode(plainText), injectedPid);
    });
    this.CommunicationTimer.Start();
  }

  public void StopCommunication()
  {
    if (this.CommunicationTimer != null)
    {
      this.CommunicationTimer.Stop();
      this.CommunicationTimer = (Timer) null;
    }
    if (this.decompilerProcess != null)
    {
      this.decompilerProcess.Kill();
      this.decompilerProcess.Dispose();
      this.decompilerProcess = (Process) null;
    }
    this.injected_pids.Clear();
  }

  public bool IsAttached(int pid) => this.injected_pids.Contains(pid);

  public async Task<VelocityStates> Attach(int pid)
  {
    if (this.injected_pids.Contains(pid))
      return VelocityStates.Attached;
    this.VelocityStatus = VelocityStates.Attaching;
    var process = Process.Start(new ProcessStartInfo()
    {
      FileName = "Bin\\erto3e4rortoergn.exe",
      Arguments = $"{pid}",
      CreateNoWindow = false,
      UseShellExecute = false,
      RedirectStandardError = false,
      RedirectStandardOutput = false
    });

    if (process != null)
    {
      await Task.Run(() => process.WaitForExit());
    }
    this.injected_pids.Add(pid);
    this.VelocityStatus = VelocityStates.Attached;
    return VelocityStates.Attached;
  }

  public VelocityStates Execute(string script)
  {
    if (this.injected_pids.Count.Equals(0))
      return VelocityStates.NotAttached;
    foreach (int injectedPid in this.injected_pids)
      NamedPipes.LuaPipe(VelAPI.Base64Encode(script), injectedPid);
    return VelocityStates.Executed;
  }
}
