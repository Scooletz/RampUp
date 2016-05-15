Framework "4.6"

properties {
  $path = "src\RampUp.sln"
}

function Get-Version {
    return (cat .specs\Version.txt)
}

Task Compile -Depends Clear, Restore {
	exec {msbuild $path /t:Rebuild /p:Configuration=Release /verbosity:normal /m /nr:false}

    if ($lastexitcode -ne 0) {
        throw "Build failed"
    }
}

Task default -Depends Compile, Tests

Task Restore {
	exec {.tools\nuget\NuGet.exe restore $path}
}

Task Clear {
	Get-ChildItem .\ -include bin,obj -Recurse | foreach ($_) { 
        remove-item $_.fullname -Force -Recurse 
    }

    Remove-Item *.nupkg
    Remove-Item *TestResult.xml
    Remove-Item *_temp.nuspec
}

Task Tests {
    src\packages\NUnit.Runners.2.6.4\tools\nunit-console.exe src\RampUp.Tests\bin\Release\RampUp.Tests.dll
    
    if ($lastexitcode -ne 0) {
        throw "Tests failed"
    }
}

Task Pack {
    $major_minor = Get-Version
    $version = $major_minor + "." + $buildNo + "-prerelease"

    Get-ChildItem .\.specs -Filter *.nuspec | foreach ($_) { 
        $tempFileName = $_.name + "_temp.nuspec"
        
        Copy-Item $_.fullname $tempFileName
        $tempFile = gi $tempFileName

        [xml] $spec = gc $tempFileName
        $spec.package.metadata.version = $version
        # $spec.package.metadata.tags = $tags

        Write-Host $tempFile
        $spec.Save($tempFile.FullName)

        # run packaging
        exec { .tools\nuget\NuGet.exe pack $tempFile.FullName -Verbosity normal -NoPackageAnalysis }
        
        # remove temp nuspec
        ri $tempFile.FullName
    }
}

Task Push {
    Get-ChildItem . -Filter *.nupkg | foreach ($_) { 
        exec { .tools\nuget\NuGet.exe push $_.fullname -Source $nugetSource -ApiKey $nugetApiKey }
        Remove-Item $_.fullname
    }
}