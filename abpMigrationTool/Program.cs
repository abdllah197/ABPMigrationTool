using System.Drawing;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

while (true)
{
    var appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
    if (Directory.Exists(appDir)) //return message here check this
    {
        string appName = "abpMigrationTool"; //app folder name
        string fileName = "Settings.json"; //seed URLs json file
        var jsonFileDir = Path.GetFullPath(Path.Combine(appDir, @"..\..\..\", fileName));
        using StreamReader reader = new(jsonFileDir);
        var jsonData = reader.ReadToEnd();
        var settings = JsonSerializer.Deserialize<Settings>(jsonData);
        if (settings != null)
        {
            if (Directory.Exists(settings.ServicesDir))
            {
                ConsoleWriteLine(1, ConsoleColor.Blue, "----Abp Migration Tool----");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "1- Add migration only after delete old migration.");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "2- Add migration with database update after delete old migration.");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "3- Delete migration only.");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "4- Run shared migration.");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "5- Run services seed.");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "6- Update database Only.");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "7- Run all operations.");
                ConsoleWriteLine(1, ConsoleColor.DarkYellow, "8- Exit.");

                ConsoleWriteLine(0, ConsoleColor.Cyan, "Command Number: ");
                var menuInput = Console.ReadLine();
                ConsoleWriteLine(3, ConsoleColor.White, string.Empty);

                bool
                    addMigration,
                    databaseUpdate,
                    sharedDbMigrator,
                    deleteMigration,
                    servicesSeed;

                addMigration =
                    databaseUpdate =
                    deleteMigration =
                    sharedDbMigrator =
                    servicesSeed =
                    false;

                switch (menuInput)
                {
                    case "1":
                        deleteMigration = true;
                        addMigration = true;
                        break;
                    case "2":
                        deleteMigration = true;
                        addMigration = true;
                        databaseUpdate = true;
                        break;
                    case "3":
                        deleteMigration = true;
                        break;
                    case "4":
                        sharedDbMigrator = true;
                        break;
                    case "5":
                        servicesSeed = true;
                        break;
                    case "6":
                        databaseUpdate = true;
                        break;
                    case "7":
                        deleteMigration = true;
                        addMigration = true;
                        databaseUpdate = true;
                        sharedDbMigrator = true;
                        servicesSeed = true;
                        break;
                    case "8":
                        Environment.Exit(0);
                        break;
                    default:
                        CustomMessage("Input not valid", ConsoleColor.Red, false);
                        continue;
                }

                var servicesDir = settings.ServicesDir;
                var servicesDirList = Directory.GetDirectories(servicesDir);

                if (deleteMigration || addMigration || databaseUpdate)
                {
                    foreach (var serviceDir in servicesDirList.Where(serviceDir => !serviceDir.Contains(appName)))//exclude App Name
                    {
                        var serviceFolderList = Directory.GetDirectories(serviceDir);
                        var serviceFolder = serviceFolderList.FirstOrDefault(serviceFolder => serviceFolder.Contains("\\src"));
                        if (Directory.Exists(serviceFolder))
                        {
                            var projectsDirList = Directory.GetDirectories(serviceFolder);
                            var projectDir = projectsDirList.FirstOrDefault(projectDir => projectDir.Contains(".EntityFrameworkCore"));
                            if (Directory.Exists(projectDir))
                            {
                                ConsoleWriteLine(1, ConsoleColor.Cyan, $"Current Service Name : {serviceDir.Split("\\").Last()}");
                                if (deleteMigration)
                                {
                                    var projectFolderList = Directory.GetDirectories(projectDir);
                                    ConsoleWriteLine(1, ConsoleColor.Magenta, "Checking Migration Folder....");
                                    var projectFolder = projectFolderList.FirstOrDefault(projectFolder => projectFolder.Contains("Migrations"));
                                    if (Directory.Exists(projectFolder))
                                    {
                                        //projectFolder => Migration Folder
                                        //projectDir => EntityFrameworkCore Folder
                                        if (Directory.Exists(projectFolder))
                                        {
                                            Directory.Delete(projectFolder, true);
                                            ConsoleWriteLine(1, ConsoleColor.Green, "Migration Folder Deleted.");
                                        }
                                    }
                                    else
                                    {
                                        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "No Migration Folder Found.");
                                        ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                                    }
                                }
                                if (addMigration)
                                {
                                    ConsoleWriteLine(1, ConsoleColor.Magenta, "Adding Migration....");
                                    RunCmdCommand(projectDir, $"dotnet ef migrations add {settings.MigratorName}");
                                    ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                                }

                                if (databaseUpdate)
                                {
                                    ConsoleWriteLine(1, ConsoleColor.Magenta, "Updating Database....");
                                    RunCmdCommand(projectDir, "dotnet ef database update");
                                    ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                                }
                            }
                        }
                    }
                }
                if (sharedDbMigrator)
                {
                    if (Directory.Exists(settings.DbMigratorDir))
                    {
                        RunCmdCommand(settings.DbMigratorDir, "dotnet run");
                    }
                    else
                    {
                        ConsoleWriteLine(1, ConsoleColor.Red, "\nDbMigrator directory is not valid.... Press any key to back to menu.");
                    }
                    ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                }
                if (servicesSeed)
                {
                    await RunSeeds(settings);
                    ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                }
                CustomMessage("\nTask ended.... Press any key to back to menu.", ConsoleColor.Green, true);                
            }
            else
            {
                CustomMessage("\nServices directory is not valid.... Press any key to back to menu.", ConsoleColor.Red, true);                
            }
        }
        else
        {
            CustomMessage("\nCan not read Settings.json or file missing.... Press any key to back to menu.", ConsoleColor.Red, true);            
        }
    }
    else
    {
        CustomMessage("\nFolders dir not Found.... Press any key to back to menu.", ConsoleColor.Red, true);       
    }

}

string? CustomMessage(string message,ConsoleColor color,bool wait)
{
    string? reply = string.Empty;
    ConsoleWriteLine(1, color, message);
    if (wait) { reply=Console.ReadLine(); }
    ConsoleWriteLine(3, ConsoleColor.White, string.Empty);
    return reply;
}

async Task<bool> RunSeeds(Settings settings)
{ 
    
    if (settings is not null && settings.SeedURLs is not null && settings.SeedURLs.Any())
    {
        if (CheckURLs(settings.SeedURLs))
        {
            foreach (var seedUrl in settings.SeedURLs.OrderBy(item => item.ServiceOrder))
            {
                ConsoleWriteLine(1, ConsoleColor.Magenta, $"Seeding Service : {seedUrl.ServiceName}....");

                var state = await WebClient(seedUrl.URL??"", settings.Token??"");
                if (state)
                {
                    ConsoleWriteLine(1, ConsoleColor.Green, $"Seeding Service : {seedUrl.ServiceName} completed.");
                    ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                    Thread.Sleep(2000);
                    continue;
                }
                else
                {
                    ConsoleWriteLine(1, ConsoleColor.Red, $"Seeding Service : {seedUrl.ServiceName} failed please check seed URL.");
                    break;
                }
            }
        }
        return true;
    }
    else
    {
        ConsoleWriteLine(1, ConsoleColor.Red, "Json file is empty or not found.");
        return true;
    }
}

async Task<bool> WebClient(string url,string token="")
{
    try 
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization=AuthenticationHeaderValue.Parse($"bearer {token}");
            client.Timeout= TimeSpan.FromSeconds(10);
            var response = await client.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        return false;
    }
    catch (Exception)
    {
        return false;
    }    
}
bool CheckURLs(List<SeedURL> seedURLs)
{
    bool result = true;
    foreach (var seedUrl in seedURLs)
    {
        Uri? uriResult;
        var isValidURL = Uri.TryCreate(seedUrl.URL, UriKind.Absolute, out uriResult)
           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        if (!isValidURL)
        {
            ConsoleWriteLine(1, ConsoleColor.Red, $"Service : {seedUrl.ServiceName} URL not valid.");
        }
        result = result && isValidURL;
    }
    return result;
}
void ConsoleWriteLine(int enterLineNumber, ConsoleColor color, string message)
{
    Console.ForegroundColor = color;
    if (enterLineNumber == 0)
    {
        Console.Write(message);
    }
    while (enterLineNumber > 0)
    {
        Console.WriteLine(message);
        --enterLineNumber;
    }
    Console.ResetColor();
}
void RunCmdCommand(string workingDirectory, string command)
{
    System.Diagnostics.Process process = new System.Diagnostics.Process();
    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
    startInfo.WorkingDirectory = workingDirectory;
    startInfo.FileName = "cmd.exe";
    startInfo.Arguments = "/C " + command;
    process.StartInfo = startInfo;
    process.Start();
    process.WaitForExit();
}
public class SeedURL
{
    public string? ServiceName { get; set; }
    public string? URL { get; set; }
    public int? ServiceOrder { get; set; }
}
public class Settings
{
    public string? Token { get; set; }
    public List<SeedURL>? SeedURLs { get; set; }
    public string? MigratorName { get; set; }
    public string? ServicesDir { get; set; }
    public string? DbMigratorDir { get; set; }
}