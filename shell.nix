{ pkgs ? import <nixpkgs> {} }:

pkgs.mkShell {
  buildInputs = with pkgs; [
    # .NET SDK (includes runtime and CLI tools)
    dotnet-sdk
    dotnet-runtime

    # Additional development tools
    nuget
    omnisharp-roslyn  # Language server for editors
    
    # Optional but useful
    git
    curl
    wget
    
    # For debugging and profiling
    gdb
    valgrind
    netcoredbg
  ];

  # Environment variables
  shellHook = ''
    # Disable .NET telemetry
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    
    # Set up NuGet config to use system packages when possible
    export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    
    # Improve build performance
    export DOTNET_CLI_UI_LANGUAGE=en-US
    
    echo "🚀 .NET development environment ready!"
    echo "📦 .NET SDK version: $(dotnet --version)"

    export TMPDIR=/tmp
    export TEMP=/tmp
    export TMP=/tmp
  '';

  # Set up SSL certificates for NuGet
  NIX_SSL_CERT_FILE = "${pkgs.cacert}/etc/ssl/certs/ca-bundle.crt";
}
