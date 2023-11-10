using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

while (true)
{
    var appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
    if (Directory.Exists(appDir)) //return message here check this
    {
        ConsoleWriteLine(1, ConsoleColor.Blue, "----Abp Migration Tool----");
        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "1- Add migration only after delete old migration.");
        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "2- Add migration with database update after delete old migration.");
        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "3- Delete migration only.");
        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "4- Run shared migration.");
        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "5- Run services seed.");
        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "6- Run all operations.");
        ConsoleWriteLine(1, ConsoleColor.DarkYellow, "7- Exit.");

        ConsoleWriteLine(0, ConsoleColor.Cyan, "Command Number: ");
        var menuInput = Console.ReadLine();
        ConsoleWriteLine(2, ConsoleColor.White, string.Empty);


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



                bool
                    addMigration,
                    databaseUpdate,
                    sharedDbMigrator,
                    deleteOnly,
                    servicesSeed;

                addMigration =
                    databaseUpdate =
                    deleteOnly =
                    sharedDbMigrator =
                    servicesSeed =
                    false;

                switch (menuInput)
                {
                    case "1":
                        addMigration = true;
                        break;
                    case "2":
                        addMigration = true;
                        databaseUpdate = true;
                        break;
                    case "3":
                        deleteOnly = true;
                        break;
                    case "4":
                        sharedDbMigrator = true;
                        break;
                    case "5":
                        servicesSeed = true;
                        break;
                    case "6":
                        addMigration = true;
                        databaseUpdate = true;
                        sharedDbMigrator = true;
                        servicesSeed = true;
                        break;
                    case "7":
                        Environment.Exit(0);
                        break;
                    default:
                        ConsoleWriteLine(1, ConsoleColor.Red, "Input not valid");
                        ConsoleWriteLine(3, ConsoleColor.White, "");
                        continue;
                }



                var servicesDir = settings.ServicesDir; 
                var servicesDirList = Directory.GetDirectories(servicesDir);

                if (addMigration || deleteOnly)
                {
                    foreach (var serviceDir in servicesDirList)
                    {
                        if (!serviceDir.Contains(appName)) //exclude app name
                        {
                            var serviceFolderList = Directory.GetDirectories(serviceDir);
                            foreach (var serviceFolder in serviceFolderList)
                            {
                                if (serviceFolder.Contains("\\src"))
                                {
                                    var projectsDirList = Directory.GetDirectories(serviceFolder);
                                    foreach (var projectDir in projectsDirList)
                                    {
                                        if (projectDir.Contains(".EntityFrameworkCore"))
                                        {
                                            var serviceSplitDir = serviceDir.Split("\\");
                                            ConsoleWriteLine(1, ConsoleColor.Cyan, $"Current Service Name : {serviceSplitDir.Last()}");
                                            var projectFolderList = Directory.GetDirectories(projectDir);

                                            ConsoleWriteLine(1, ConsoleColor.Magenta, "Checking Migration Folder....");

                                            foreach (var projectFolder in projectFolderList)
                                            {
                                                if (projectFolder.Contains("Migrations"))
                                                {
                                                    //projectFolder => Migration Folder
                                                    //projectDir => EntityFrameworkCore Folder
                                                    if (Directory.Exists(projectFolder))
                                                    {
                                                        Directory.Delete(projectFolder, true);
                                                        ConsoleWriteLine(1, ConsoleColor.Green, "Migration Folder Deleted.");
                                                    }
                                                }
                                            }
                                            if (addMigration)
                                            {
                                                ConsoleWriteLine(1, ConsoleColor.Magenta, "Adding Migration....");
                                                RunCmdCommand(projectDir, "dotnet ef migrations add Initial");
                                            }
                                            ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                                            if (databaseUpdate)
                                            {
                                                ConsoleWriteLine(1, ConsoleColor.Magenta, "Updating Database....");
                                                RunCmdCommand(projectDir, "dotnet ef database update");
                                            }
                                            ConsoleWriteLine(1, ConsoleColor.White, string.Empty);

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (sharedDbMigrator)
                {
                    var projectFoldersDir = Path.GetFullPath(Path.Combine(appDir, @"..\..\..\..\..\")); //return navigation back to sharedDbMigrator dir
                    var projectFolderDirList = Directory.GetDirectories(projectFoldersDir);

                    foreach (var projectFolderDir in projectFolderDirList)
                    {
                        if (projectFolderDir.Contains("\\shared"))
                        {
                            var sharedDirList = Directory.GetDirectories(projectFolderDir);
                            foreach (var sharedDir in sharedDirList)
                            {
                                if (sharedDir.Contains(".DbMigrator"))
                                {
                                    RunCmdCommand(sharedDir, "dotnet run");
                                }
                            }
                        }
                    }
                    ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                }
                if (servicesSeed)
                {
                    await RunSeeds(settings);
                    ConsoleWriteLine(1, ConsoleColor.White, string.Empty);
                }

                ConsoleWriteLine(1, ConsoleColor.Green, "\nTask ended.... Press any key to back to menu.");
                Console.ReadLine();
                ConsoleWriteLine(3, ConsoleColor.White, string.Empty);
            }
            else
            {
                ConsoleWriteLine(1, ConsoleColor.Red, "\nServices directory is not valid.... Press any key to back to menu.");
                Console.ReadLine();
                ConsoleWriteLine(3, ConsoleColor.White, string.Empty);
            }
        }
        else
        {
            ConsoleWriteLine(1, ConsoleColor.Red, "\nCan not read Settings.json or file missing.... Press any key to back to menu.");
            Console.ReadLine();
            ConsoleWriteLine(3, ConsoleColor.White, string.Empty);
        }
    }
    else
    {
        ConsoleWriteLine(1, ConsoleColor.Red, "\nFolders dir not Found.... Press any key to back to menu.");
        Console.ReadLine();
        ConsoleWriteLine(3, ConsoleColor.White, string.Empty);
    }

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
    public string? ServicesDir { get; set; }
}