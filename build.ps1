$ErrorActionPreference = "Stop"

if($args.Count -ne 1){
    Write-Host "You need to enter the version."
    exit
}

$splitedVersion = $args[0] -split "\."
if($splitedVersion.Count -ne 3){
    Write-Host "You entered invalid version."
    exit
}

foreach($splited in $splitedVersion){
    $_num = $null
    if(-not([int]::TryParse($splited,[ref]$_num))){
        Write-Host "You entered invalid version."
        exit
    }
}

$tags = git tag

if(($null -ne $tags) -and $tags.Contains($args[0])){
    Write-Host "That version already exist."
    exit
}

$name = "NuGetImporterForUnity." + $args[0]
$packageProjectPath = Convert-Path "NuGetImporterForUnity"
$exportProjectPath = Convert-Path "Packager"

$packageDotJsonContents = `
    $(Get-Content "NuGetImporterForUnity/Packages/NuGet Importer/package.json") `
    -replace """version"": ""\d\.\d\.\d""" , """version"": ""${args[0]}"""
$packageDotJsonContents > "NuGetImporterForUnity/Packages/NuGet Importer/package.json"

$asmVersion = $splitedVersion[0] + "." + $splitedVersion[1] + ".0." + $splitedVersion[2]
$AssemblyInfoContents = `
    $(Get-Content "NuGetImporterForUnity/Packages/NuGet Importer/Editor/AssemblyInfo.cs") `
    -replace "\[assembly: AssemblyVersion\(""\d\.\d\.\d\.\d""\)\]" , "[assembly: AssemblyVersion(""$asmVersion"")]"
$AssemblyInfoContents > "NuGetImporterForUnity/Packages/NuGet Importer/Editor/AssemblyInfo.cs"

Start-Process -FilePath $env:UNITY_2019_4_14f1 `
    -ArgumentList "-projectPath ""${packageProjectPath}"" -batchmode -nographics -quit" `
    -Wait

Copy-Item -Path "NuGetImporterForUnity/Library/ScriptAssemblies/kumaS.NuGetImporter.Editor.dll" `
    -Destination "Packager/Assets/NuGet importer/Editor/kumaS.NuGetImporter.Editor.dll" `
    -Force

New-Item -Path "Release/$name" -ItemType Directory

Start-Process -FilePath $env:UNITY_2019_4_14f1 `
    -ArgumentList "-projectPath ""${exportProjectPath}"" -batchmode -nographics -exportPackage ""Assets/NuGet Importer"" ""../Release/$name/$name.unitypackage"" -quit" `
    -Wait

Copy-Item "NuGetImporterForUnity/Packages/NuGet Importer/Documentation~" `
    -Destination "Release/$name" -Recurse -Force
Copy-Item "NuGetImporterForUnity/Packages/NuGet Importer/LICENSE.md" `
    -Destination "Release/$name/LICENSE.md" -Force
Compress-Archive -Path "Release/$name/$name.unitypackage" , "Release/$name/Documentation~" , "Release/$name/LICENSE.md" `
    -DestinationPath "Release/$name.zip" -Force

docfx docFX/docfx.json

git add -A
git commit -m "Release ${args[0]}"
git tag $args[0]