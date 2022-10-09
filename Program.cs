using System.IO.Compression;

if (args.Length <= 0)
{
    Console.WriteLine("No file path specified!");
    return 1;
}
else if (args.Length > 1)
{
    Console.WriteLine("Too many arguments! Only file path is required.");
    return 1;
}

var sourcePath = args[0];

// Convert single zip to cbz
if (File.Exists(sourcePath))
{
    return ConvertZipToCbz(sourcePath);
}
// Convert all zip file in directory to cbz
else if (Directory.Exists(sourcePath))
{
    foreach (var file in Directory.EnumerateFiles(sourcePath, "*.zip", SearchOption.AllDirectories))
    {
        int result = ConvertZipToCbz(file);
        if (result != 0)
            return result;
    }
    return 0;
}
else
{
    Console.WriteLine($"No file or directory at path: \"{sourcePath}\".");
    return 1;
}

static int ConvertZipToCbz(string sourcePath)
{
    Console.WriteLine($"Converting file at path: \"{sourcePath}\".");

    // Will extract to temporary subfolder folder
    var extractPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(sourcePath));

    // Delete temporary folder if already exists
    if (Directory.Exists(extractPath))
        Directory.Delete(extractPath, true);

    ZipFile.ExtractToDirectory(sourcePath, extractPath);

    // List extracted file names
    var subDirs = Directory.GetDirectories(extractPath, "*_files", SearchOption.TopDirectoryOnly);
    if (subDirs.Length == 0)
    {
        Console.WriteLine("Could not find _files directory - zip file does not match expected structure.");
        return 1;
    }

    // Assume there's only one files subdirectory
    var filesDir = subDirs[0];

    // Move cover image out to root
    var coverPath = Path.Combine(filesDir, "cover.jpeg");
    File.Move(coverPath, Path.Combine(extractPath, "00000.jpeg"));

    // Move other images out to root
    var imagesPath = Path.Combine(filesDir, "images");
    foreach (var imagePath in Directory.GetFiles(imagesPath))
    {
        var imageName = Path.GetFileName(imagePath);
        File.Move(imagePath, Path.Combine(extractPath, imageName));
    }

    // Delete files subdir
    var filesDirNameSplit = Path.GetFileName(filesDir)!.Split('_', 2);
    Directory.Delete(filesDir, true);

    // Delete matching html file
    var htmlPath = Path.Combine(extractPath, filesDirNameSplit[0]);
    File.Delete($"{htmlPath}.html");

    // Ensure new archive path is available
    var newPath = $"{sourcePath[0..^3]}cbz";
    if (File.Exists(newPath))
    {
        Console.WriteLine($"Deleting existing file: {newPath}");
        File.Delete(newPath);
    }

    // Write new archive
    Console.WriteLine($"Writing new archive to: {newPath}");
    ZipFile.CreateFromDirectory(extractPath, newPath);

    // Delete source
    Console.WriteLine($"Deleting source file at path: \"{sourcePath}\".");
    File.Delete(sourcePath);

    return 0;
}