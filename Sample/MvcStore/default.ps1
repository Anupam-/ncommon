properties {
  $base_dir = Resolve-Path .
  $sln = "$base_dir\MvcStore.sln"
  $build_config = "debug"
  $nuget = "$base_dir\..\..\Tools\nuget.exe"
  $packages_dir = "$base_dir\packages"
  $version = "1.2"
}

$framework = "4.0"

Task default -depends build

Task init {
    foreach($file in Get-ChildItem -Include 'packages.config' -Recurse) {
        Write-Host "Installing packages from $file"
        exec{nuget install -OutputDirectory $packages_dir $file.FullName}
    }
    Write-Host "Finished initializing build."
}

Task build -depends init {
    exec {msbuild $sln /verbosity:minimal "/p:Config=$build_config" /nologo}
}