using Azure.Storage.Blobs.Models;
using BlobBackup;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;
using Index = BlobBackup.Index;

var stopwatch = Stopwatch.StartNew();
var app = new CommandLineApplication
{
    Name = "blob-backup",
};
AddHelp(app);

app.Description = "Backup or restore a folder.";
var accessTier = AddAccessTier(app);
var container = AddContainer(app);
var createContainer = AddCreateContainer(app);
var changeKey = AddChangeKey(app);
var copyShards = AddCopyShards(app);
var deleteSecrets = AddDeleteSecrets(app);
var fake = AddFake(app);
var noAzure = AddNoAzure(app);
var noCompression = AddNoCompression(app);
var printIndex = AddPrintIndex(app);
var restore = AddRestore(app);
var restorePrefix = AddRestorePrefix(app);
var resetIndex = AddResetIndex(app);
var secretsIdentifier = AddSecretsIdentifier(app);
var secretsFolder = AddSecretsFolder(app);
var setMainKey = AddSetMainKey(app);
var shardSize = AddShardSize(app);
var verbose = AddVerbose(app);
var sevenZip = AddSevenZip(app);
var fileSystemFolder = app.Argument("file-system-folder", "Required. The folder to backup or restore.");
var blobStorageFolder = app.Argument("blob-storage-folder", "Required. The blob storage folder to use.");

app.OnExecute(async () =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(fileSystemFolder.Value))
        {
            app.ShowHelp();

            return 1;
        }

        var serviceProvider = await ServiceProviderProvider.CreateServiceProvider(
            accessTier: GetAccessTier(accessTier),
            fileSystemFolder: GetValue(fileSystemFolder),
            container: GetContainer(container),
            blobStorageFolder: GetValue(blobStorageFolder),
            restorePrefix: TryGetValue(restorePrefix)?.Replace('\\', '/'),
            resetIndex: resetIndex.HasValue(),
            createContainer: createContainer.HasValue(),
            fake: fake.HasValue(),
            sevenZip: sevenZip.HasValue(),
            noCompression: noCompression.HasValue(),
            noAzure: noAzure.HasValue(),
            deleteSecrets: deleteSecrets.HasValue(),
            secretsFolder: GetSecretsFolder(secretsIdentifier, secretsFolder),
            shardSizeBytes: GetShardSizeBytes(shardSize),
            verbose: verbose.HasValue());
        var mainCipherKeyLoader = serviceProvider.GetRequiredService<MainCipherKeyLoader>();

        if (setMainKey.HasValue())
        {
            await mainCipherKeyLoader.SetMainCipherKey();
        }
        else if (changeKey.HasValue())
        {
            await mainCipherKeyLoader.ChangeCipherKey();
        }
        else if (printIndex.HasValue())
        {
            await mainCipherKeyLoader.LoadMainCipherKey();
            var index = serviceProvider.GetRequiredService<Index>();
            await index.LoadIndex();
            Console.WriteLine(JsonSerializer.Serialize(
                index.FileTokens.Select(token => token.Key.RelativePath),
                new JsonSerializerOptions { WriteIndented = true }));
        }
        else if (copyShards.HasValue())
        {
            await serviceProvider.GetRequiredService<RestoreProvider>().CopyShards(stopwatch);
        }
        else if (restore.HasValue())
        {
            if (fake.HasValue())
            {
                Helper.WriteLine("Not running restore because the '--fake' flag is found.");
            }
            else
            {
                await mainCipherKeyLoader.LoadMainCipherKey();
                await serviceProvider.GetRequiredService<RestoreProvider>().RestoreFileSystemFolder(stopwatch);
            }
        }
        else
        {
            await mainCipherKeyLoader.LoadMainCipherKey();
            await serviceProvider.GetRequiredService<BackupProvider>().BackupFileSystemFolder(stopwatch);
        }

        return 0;
    }
    catch (Exception ex)
    {
        return HandleException(ex);
    }
});

try
{
    return app.Execute(args);
}
catch (Exception ex)
{
    return HandleException(ex);
}

static int HandleException(Exception ex)
{
    Helper.WriteLine();
    Helper.WriteLine($"Error: {ex}");
    Helper.WriteLine("Use --delete-secrets to delete the saved connection string and cipher key.");

    return 1;
}

static void AddHelp(CommandLineApplication app) => app.HelpOption("-?|-h|--help");

static CommandOption AddAccessTier(CommandLineApplication command)
{
    return command.Option(
        "-at|--access-tier <tier>",
        "The access tier to use for the shards. Possible values: archive, cool, hot. Archive is the default value.",
        CommandOptionType.SingleValue);
}

static CommandOption AddContainer(CommandLineApplication command)
{
    return command.Option(
        "-c|--container <name>",
        "The name of the blob container to use. Defaults to 'blob-backup'.",
        CommandOptionType.SingleValue);
}

static CommandOption AddCreateContainer(CommandLineApplication command)
{
    return command.Option(
        "-cc|--create-container",
        "Create the blob container (if it doesn't exist) before any other operation is done.",
        CommandOptionType.NoValue);
}

static CommandOption AddChangeKey(CommandLineApplication command)
{
    return command.Option(
        "-ck|--change-key",
        "Change the cipher key. No backup or restore operation is done. " +
        "After changing the cipher key you manually need to remove the old main cipher key backup, " +
        "otherwise you can still obtain the main cipher key by using the old cipher key and the backup.",
        CommandOptionType.NoValue);
}

static CommandOption AddCopyShards(CommandLineApplication command)
{
    return command.Option(
        "-cs|--copy-shards",
        "Copy the shards to the hot access tier. No backup or restore operation is done.",
        CommandOptionType.NoValue);
}

static CommandOption AddDeleteSecrets(CommandLineApplication command)
{
    return command.Option(
        "-ds|--delete-secrets",
        "Delete the saved secrets so that new secrets can be submitted. " +
        "Only the secrets for the current secrets identifier (see '--secret') are deleted.",
        CommandOptionType.NoValue);
}

static CommandOption AddFake(CommandLineApplication command)
{
    return command.Option(
        "-f|--fake",
        "Do not upload anything, but show what would have been done.",
        CommandOptionType.NoValue);
}

static CommandOption AddNoAzure(CommandLineApplication command)
{
    return command.Option(
        "-na|--no-azure",
        "Do not use Azure but use the file system instead.",
        CommandOptionType.NoValue);
}

static CommandOption AddNoCompression(CommandLineApplication command)
{
    return command.Option(
        "-nc|--no-compression",
        "Do not compress shards.",
        CommandOptionType.NoValue);
}

static CommandOption AddPrintIndex(CommandLineApplication command)
{
    return command.Option(
        "-pi|--print-index",
        "Print the index. No backup or restore is done.",
        CommandOptionType.NoValue);
}

static CommandOption AddRestore(CommandLineApplication command)
{
    return command.Option(
        "-r|--restore",
        "Restore the file system folder (instead of doing a backup). " +
        "The file system folder needs to be empty.",
        CommandOptionType.NoValue);
}

static CommandOption AddResetIndex(CommandLineApplication command)
{
    return command.Option(
        "-ri|--reset-index",
        "Replace the shard tokens in the index by the shard tokens found in the \"shard-tokens\" folder in the blob storage. " +
        "Also, file tokens that reference non-existing chunks are deleted.",
        CommandOptionType.NoValue);
}

static CommandOption AddSecretsIdentifier(CommandLineApplication command)
{
    return command.Option(
        "-s|--secrets <identifier>",
        "The secrets to use. For each identifier the secrets are stored separately.",
        CommandOptionType.SingleValue);
}

static CommandOption AddSecretsFolder(CommandLineApplication command)
{
    return command.Option(
        "-sf|--secrets-folder <path>",
        "The folder to store the secrets.",
        CommandOptionType.SingleValue);
}

static CommandOption AddSetMainKey(CommandLineApplication command)
{
    return command.Option(
        "-smk|--set-main-key",
        "Set the main cipher key. " +
        "This command is only for debugging, as you normally do not know the main cipher key. " +
        "No backup or restore is done.",
        CommandOptionType.NoValue);
}

static CommandOption AddShardSize(CommandLineApplication command)
{
    return command.Option(
        "-ss|--shard-size <MiB>",
        "The target shard size. " +
        "Defaults to 512 MiB. " +
        "If you change the shard size of an existing backup then files larger than the shard size need to be uploaded " +
        "again after an index reset.",
        CommandOptionType.SingleValue);
}

static CommandOption AddVerbose(CommandLineApplication command)
{
    return command.Option(
        "-v|--verbose",
        "Print debugging information.",
        CommandOptionType.NoValue);
}

static CommandOption AddSevenZip(CommandLineApplication command)
{
    return command.Option(
        "-7|--7zip",
        "Use 7-Zip to compress the files. " +
        "To use this feature the program 7-Zip needs to be installed on the system and the PATH environment variable needs to include " +
        "the path to the '7z' executable.",
        CommandOptionType.NoValue);
}

static CommandOption AddRestorePrefix(CommandLineApplication command)
{
    return command.Option(
        "-rp|--restore-prefix <path>",
        "Only restore the files of which the relative path starts with the given path.",
        CommandOptionType.SingleValue);
}

static string? TryGetValue(CommandOption commandOption)
{
    if (commandOption.HasValue())
    {
        var value = commandOption.Value();

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return null;
}

static string GetValue(CommandArgument commandArgument)
{
    if (string.IsNullOrWhiteSpace(commandArgument.Value))
    {
        throw new Exception($"Argument '{commandArgument.Name}' not found or empty.");
    }

    return commandArgument.Value;
}

static string GetSecretsFolder(CommandOption secretsIdentifier, CommandOption secretsFolder)
{
    var baseFolder = TryGetValue(secretsFolder) ?? ServiceProviderProvider.DefaultDataFolder;

    return Path.Combine(baseFolder, TryGetValue(secretsIdentifier) ?? "default");
}

static int GetShardSizeBytes(CommandOption shardSize)
{
    if (shardSize.HasValue())
    {
        var number = int.Parse(shardSize.Value());

        if (number <= 0)
        {
            throw new Exception($"Invalid value found.");
        }

        return number * 1048576;
    }

    return 536870912; // 512 MiB
}

static AccessTier GetAccessTier(CommandOption accessTier)
{
    if (TryGetValue(accessTier) is string value)
    {
        return value.ToLowerInvariant() switch
        {
            "archive" => AccessTier.Archive,
            "cool" => AccessTier.Cool,
            "hot" => AccessTier.Hot,
            _ => throw new Exception($"Cannot parse '{value}' as access tier."),
        };
    }

    return AccessTier.Archive;
}

static string GetContainer(CommandOption container) => TryGetValue(container) ?? "blob-storage";