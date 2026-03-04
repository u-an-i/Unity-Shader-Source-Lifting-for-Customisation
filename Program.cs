using System.Reflection.Metadata;
using System.Text.RegularExpressions;


static void help()
{
    Console.WriteLine("\nparameter:\n\n\t--root\t\tpath to folder which contains the CGIncludes folder and the shader source folders\n\t--name\t\tname for custom reference to be used in shader names and CGIncludes\n\t--out\t\toptional path to folder to contain the modified copy, defaults to root, creates folder \"uplift\"\n\t\t\ttherein\n\t--collect\toptional path, relative to root, of file of which and of whose referenced and\n\t\t\trecursively referenced files only, lifting shall occur\n");
}

if (args.Length < 2 || args.Length > 8 || args.Length % 2 == 1)
{
    help();
    return 1;
}

string none = "";

string pathRoot = "", pathOut = "";
string referenceName = "";
string pathCollect = "";

ref string input = ref none;

for(int i=0; i<args.Length; ++i)
{
    switch (args[i])
    {
        case "--root":
            input = ref pathRoot;
            break;
        case "--out":
            input = ref pathOut;
            break;
        case "--name":
            input = ref referenceName;
            break;
        case "--collect":
            input = ref pathCollect;
            break;
        default:
            input = args[i];
            break;
    }
}

if(pathRoot == "" || referenceName == "")
{
    help();
    return 2;
}

pathRoot = Directory.GetCurrentDirectory() + "/" + pathRoot;

if (pathOut == "")
{
    pathOut = pathRoot;
}

pathOut += "/uplift";
pathCollect = pathRoot + "/" + pathCollect;

FileInfo root = new("none");
FileInfo name = new("none");
FileInfo output = new("none");
FileInfo collect = new("none");
bool validatePaths()
{
    try
    {
        root = new(pathRoot);               // validates by creating a file object for which its name must be a valid filename which then also is a valid folder name
        name = new(referenceName);
        output = new(pathOut);
        collect = new(pathCollect);
    }
    catch
    {
        help();
        return false;
    }
    return true;
}
if(!validatePaths())
{
    return 3;
}

DirectoryInfo dir = new(root.FullName);

int countIssues = 0;
int countCopies = 0;
int countEditor = 0;

void mod(FileInfo[] files, Func<string, string> modification)
{
    foreach (FileInfo file in files)
    {
        if (file.FullName.Contains(Path.DirectorySeparatorChar + "EditorDefaultResources" + Path.DirectorySeparatorChar))
        {
            ++countEditor;
            continue;
        }
        string content = File.ReadAllText(file.FullName);
        content = modification(content);
        string dirOut = output.FullName + "/" + referenceName;
        Directory.CreateDirectory(dirOut);
        try
        {
            File.WriteAllText(dirOut + "/" + file.Name, content);
        }
        catch
        {
            ++countIssues;
        }
        ++countCopies;
    }
}

Func<string, string> shaderMod = (string content) => {
    content = Regex.Replace(content, "(Shader )\"([^\"]+)\"", "$1\"" + referenceName + "/$2\"");
    content = Regex.Replace(content, "(Fall(?:B|b)ack )\"([^\"]+)\"", "$1\"" + referenceName + "/$2\"");
    content = Regex.Replace(content, "(UsePass )\"([^\"]+)\"", "$1\"" + referenceName + "/$2\"");
    content = Regex.Replace(content, "(Dependency [^=]+= )\"([^\"]+)\"", "$1\"" + referenceName + "/$2\"");
    content = Regex.Replace(content, "(#include )\"([^\"]+.((cg|glsl)inc|hlsl))\"", "$1\"../" + referenceName + "/$2\"");
    return content;
};

Func<string, string> incMod = (string content) => {
    content = Regex.Replace(content, "(#include )\"([^\"]+.((cg|glsl)inc|hlsl))\"", "$1\"../" + referenceName + "/$2\"");
    return content;
};

Dictionary<string, bool> alreadyModdedFiles = [];

void modRecursively(FileInfo file)
{
    if (!alreadyModdedFiles.ContainsKey(file.FullName))
    {
        alreadyModdedFiles[file.FullName] = true;
        List<FileInfo> files = [];
        string content = File.ReadAllText(file.FullName);
        Match match1 = Regex.Match(content, "(?:#include )\"([^\"]+.((cg|glsl)inc|hlsl))\"");
        if (match1.Success)
        {
            FileInfo file2 = new("none");
            try
            {
                file2 = dir.GetFiles(match1.Groups[1].Value, SearchOption.AllDirectories)[0];
            }
            catch
            { }
            if (file2.Name != "none")
                files.Add(file2);
        }
        if (file.Extension.ToLower() == ".shader")
        {
            foreach (Match match2 in new Match[]{
                Regex.Match(content, "(?:Fall(?:B|b)ack )\"([^\"]+)\""),
                Regex.Match(content, "(?:UsePass )\"([^\"]+)\""),
                Regex.Match(content, "(?:Dependency [^=]+= )\"([^\"]+)\"")
            })
            {
                if (match2.Success)
                {
                    FileInfo file2 = new("none");
                    try
                    {
                        file2 = dir.GetFiles(match2.Groups[1].Value + ".shader", SearchOption.AllDirectories)[0];
                    }
                    catch
                    { }
                    if (file2.Name != "none")
                        files.Add(file2);
                }
            }
            content = shaderMod(content);
        }
        else
        {
            content = incMod(content);
        }
        string dirOut = output.FullName + "/" + referenceName;
        Directory.CreateDirectory(dirOut);
        try
        {
            File.WriteAllText(dirOut + "/" + file.Name, content);
        }
        catch
        {
            ++countIssues;
        }
        ++countCopies;
        foreach (FileInfo file3 in files)
        {
            modRecursively(file3);
        }
    }
}

if(collect.Exists)
{
    modRecursively(collect);
}
else
{
    if(!root.Exists && dir.Exists)
    {
        FileInfo[] files = dir.GetFiles("*.shader", SearchOption.AllDirectories);
        mod(files, shaderMod);

        files = dir.GetFiles("*.cginc", SearchOption.AllDirectories);
        files = [.. files, .. dir.GetFiles("*.glslinc", SearchOption.AllDirectories)];
        files = [.. files, .. dir.GetFiles("*.hlsl", SearchOption.AllDirectories)];
        mod(files, incMod);
    }
}

Console.WriteLine(countIssues + " issues occured creating " + countCopies + " copies. Skipped " + countEditor + " Editor files.");

return 0;