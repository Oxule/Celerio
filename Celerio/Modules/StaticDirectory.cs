namespace Celerio;

public class StaticDirectory : StaticFiles
{
    public string Route;
    public string Directory;

    public void Index()
    {
        Files = new Dictionary<string, StaticFile>();
        foreach (var file in System.IO.Directory.GetFiles(Directory))
        {
            var rel = Path.GetRelativePath(Directory, file);
            string type = MIME.GetType(Path.GetExtension(file));
            Console.WriteLine($"Indexing {Route}{rel} => {file} as {type}");
            Files.Add(Route+rel, new StaticFile(file, type));
        }
    }

    public StaticDirectory(string route, string directory)
    {
        Route = route;
        Directory = directory;
        Index();
    }
}