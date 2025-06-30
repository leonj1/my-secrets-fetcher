using CommandLine;

namespace SecretsManager.Models
{
    public class CommandLineOptions
    {
        [Option('h', "help", Required = false, HelpText = "Display this help message.")]
        public bool Help { get; set; }

        [Option('v', "version", Required = false, HelpText = "Display version information.")]
        public bool Version { get; set; }

        [Option('s', "secret-name", Required = false, HelpText = "AWS Secrets Manager secret name to fetch. Overrides configuration.")]
        public string? SecretName { get; set; }

        [Option('r', "region", Required = false, HelpText = "AWS region (e.g., us-east-1). Overrides configuration.")]
        public string? Region { get; set; }

        [Option('o', "output-mode", Required = false, HelpText = "Output mode: 'env' (environment variables), 'file' (.env file), or 'both'. Default: both.")]
        public string? OutputMode { get; set; }

        [Option('e', "env-file", Required = false, HelpText = "Path to the .env file to create/update. Default: .env")]
        public string? EnvFilePath { get; set; }

        [Option('x', "env-example", Required = false, HelpText = "Path to the .env.example file to parse. Default: .env.example")]
        public string? EnvExamplePath { get; set; }

        [Option("access-key", Required = false, HelpText = "AWS access key ID. Overrides configuration.")]
        public string? AccessKey { get; set; }

        [Option("secret-key", Required = false, HelpText = "AWS secret access key. Overrides configuration.")]
        public string? SecretKey { get; set; }

        [Option('d', "devcontainer", Required = false, HelpText = "Path to devcontainer.json file. Default: .devcontainer/devcontainer.json")]
        public string? DevContainerPath { get; set; }

        [Option("quiet", Required = false, HelpText = "Suppress non-error output.")]
        public bool Quiet { get; set; }

        [Option("dry-run", Required = false, HelpText = "Show what would be done without making changes.")]
        public bool DryRun { get; set; }
    }
}