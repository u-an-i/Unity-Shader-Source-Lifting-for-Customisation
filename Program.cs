using System.Text.RegularExpressions;


static void help()
{
    Console.WriteLine("\nparameter:\n\n\t--root\t\tpath to folder which contains the CGIncludes folder and the shader source folders\n\t--name\t\tname for custom reference to be used in shader names and CGIncludes\n\t--out\t\tpath to folder to contain the modified copy, defaults to root, creates folder \"uplift\" therein\n");
}

if (args.Length < 2 || args.Length > 6 || args.Length % 2 == 1)
{
    help();
    return 1;
}

string none = "";

string pathRoot = "", pathOut = "";
string referenceName = "";

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

FileInfo root = new("none");
FileInfo name = new("none");
FileInfo output = new("none");
bool validatePaths()
{
    try
    {
        root = new(pathRoot);
        name = new(referenceName);
        output = new(pathOut);
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

FileInfo[] files = dir.GetFiles("*.shader", SearchOption.AllDirectories);
if(files.Length > 0)
{
    foreach(FileInfo file in files)
    {
        if(file.FullName.Contains(Path.DirectorySeparatorChar + "EditorDefaultResources" + Path.DirectorySeparatorChar))
        {
            ++countEditor;
            continue;
        }
        string content = File.ReadAllText(file.FullName);
        content = Regex.Replace(content, "(Shader )\"([^\"]+)\"", "$1\"" + referenceName + "/$2\"");
        content = Regex.Replace(content, "(Fallback )\"([^\"]+)\"", "$1\"" + referenceName + "/$2\"");
        content = Regex.Replace(content, "(#include )\"([^\"]+.((cg|glsl)inc|hlsl))\"", "$1\"../" + referenceName + "/$2\"");
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

files = dir.GetFiles("*.cginc", SearchOption.AllDirectories);
files = [.. files, .. dir.GetFiles("*.glslinc", SearchOption.AllDirectories)];
if (files.Length > 0)
{
    foreach (FileInfo file in files)
    {
        string content = File.ReadAllText(file.FullName);
        content = Regex.Replace(content, "(#include )\"([^\"]+.((cg|glsl)inc|hlsl))\"", "$1\"../" + referenceName + "/$2\"");
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

Console.WriteLine(countIssues + " issues occured creating " + countCopies + " copies. Skipped " + countEditor + " Editor files.");

return 0;