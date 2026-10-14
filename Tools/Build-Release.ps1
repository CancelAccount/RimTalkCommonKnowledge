<#
.SYNOPSIS
    RimTalk 常识+ 打包脚本：一次运行同时产出 Release 包与 Debug 包。

.DESCRIPTION
    版本号唯一来源为 About/About.xml 的 <modVersion>，脚本负责：
      1. 校验工作区状态与 tag 是否已存在（仅告警，不阻塞）；
      2. 对每个受支持游戏版本（$GameVersionDirs）分别以 Release / Debug 配置编译
         （输出目录由各版本 csproj 隔离：Release -> <版本>\Assemblies，Debug -> <版本>\AssembliesDebug）；
      3. 按发布目录白名单组装两个包，复制根级必需文件（LoadFolders.xml），
         开发目录（Source / AssembliesDebug 等）在复制阶段即排除；
      4. 每个版本的 dll 分别放入包内 <版本>\Assemblies（LoadFolders.xml 按游戏版本择一加载）；
      5. Debug 包把包内 <modVersion> 改写为 "<版本>-debug"，仓库原文件不动；
      6. 压缩为 dist/ 下的两个 zip（UTF-8 条目名，兼容中文文件名）。
    前置要求（已在各版本 <版本>\Source\RimTalkCommonKnowledge.csproj 中配置）：
      - Debug/Release 输出目录隔离（AssembliesDebug / Assemblies）
      - <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
      - <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>

.PARAMETER CreateRelease
    打包完成后调用 gh CLI 创建 GitHub Release 并上传两个 zip（需先 push 并已登录 gh）。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Build-Release.ps1
    powershell -ExecutionPolicy Bypass -File Tools\Build-Release.ps1 -CreateRelease

.NOTES
    本文件必须保存为 UTF-8 with BOM：Windows PowerShell 5.1 会把无 BOM 的 UTF-8 脚本按系统
    ANSI（中文系统为 GBK）解码，中文字符串乱码后连引号一起被吞掉，脚本直接报语法错误。
#>
[CmdletBinding()]
param(
    [switch]$CreateRelease
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ==== 可配置项（按目标 mod 修改）====
$PackageName    = 'RimTalkCommonKnowledge'              # 发布包顶层文件夹名（zip 解压后的目录名，通常等于 mod 文件夹名）
$AssemblyName   = 'RimTalkCommonKnowledge'              # 编译产物 dll 名（取自 csproj 项目名，不含 .dll）
# 受支持的游戏版本目录：每个版本各有一个 <版本>\Source\<AssemblyName>.csproj，
# 脚本对其分别编译并把 dll 放入包内 <版本>\Assemblies（LoadFolders.xml 按运行版本择一加载）。
# 1.6=Unity/Mono(net48)；1.7=Waylith/CoreCLR(net10.0)
$GameVersionDirs = @('1.6', '1.7')
# 本次从源码重新编译的版本（要求本机装有对应版本的游戏引用程序集）。
# 未列入的版本视为「冻结成熟版」：不编译，直接使用仓库内已入库的
# <版本>\Assemblies 与 <版本>\AssembliesDebug 现存 dll（下方会打印时间戳供发布前肉眼核对）。
# 1.6 于 1.7 发布后冻结：本机游戏升级 1.7 后旧 Managed 目录被删除，net48 工程无法引用
# 1.7 的 net10 程序集（CS1705）；将来恢复 1.6 编译环境后（Steam beta 切回 1.6，
# 或建库内引用程序集目录），把 '1.6' 加回本数组即可恢复双版本编译。
$BuildVersionDirs = @('1.7')
$RequiredDirs   = @('About', 'KnowledgeBase', 'Languages')  # 发布包必需目录（缺失即报错）
$IncludeDirs    = @('About', 'KnowledgeBase', 'Languages')  # 进入发布包的目录白名单；KnowledgeBase=常识库数据（按 modid 注入的资产，必须随包）；Languages=三语 Keyed 翻译（D69，缺了游戏内只显示键名）
$OptionalDirs   = @()                                   # 白名单中缺失时跳过而非报错的目录（本 mod 暂无需选目录）

# 根级必需文件：LoadFolders.xml 声明「根 + 各版本」布局，必须带上，否则包内容缺失
$IncludeRootFiles = @('LoadFolders.xml')

# 绝不能进发布包的开发目录（相对包根）：复制阶段即用 robocopy /XD 排除，组装后兜底自检。
# 版本目录整目录不进白名单、dll 由脚本单独放入；此处为防御性配置（若将来把版本目录加入
# $IncludeDirs 即立即生效），同时覆盖全部受支持版本（Source\obj 的句柄争抢问题见下）。
$ExcludeRelativeDirs = @()
foreach ($v in $GameVersionDirs) {
    $ExcludeRelativeDirs += "$v\Source"
    $ExcludeRelativeDirs += "$v\AssembliesDebug"
}

# 需要从包内剔除的文件模式（相对包根递归匹配）：本 mod 无本机后处理产物，留空
$ExcludeFilePatterns = @()

$DistDirName    = 'dist'                                # 打包输出目录（已加入 .gitignore）
$DebugModSuffix = 'debug'                               # 调试包版本号与文件名后缀
# ====================================

$RepoRoot   = Split-Path -Parent $PSScriptRoot
$AboutPath  = Join-Path $RepoRoot 'About\About.xml'
$DistDir    = Join-Path $RepoRoot $DistDirName
$StageDir   = Join-Path $DistDir 'stage'

<#
.SYNOPSIS 输出阶段提示。
#>
function Write-Step {
    param([string]$Text)
    Write-Host "==> $Text" -ForegroundColor Cyan
}

<#
.SYNOPSIS 从 About.xml 读取 <modVersion>（版本号唯一人工编辑点）。
#>
function Get-ModVersion {
    param([string]$Path)

    if (-not (Test-Path $Path)) { throw "未找到 About.xml：$Path" }

    [xml]$xml = Get-Content -Path $Path -Raw
    $version = $xml.ModMetaData.modVersion
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "About.xml 中缺少 <modVersion>，请先补全后再打包"
    }
    return $version.Trim()
}

<#
.SYNOPSIS 以指定配置编译单个游戏版本的工程，失败即终止整个流程。
.PARAMETER VersionDir 游戏版本目录（如 1.6 / 1.7），工程位于其 Source 子目录。
.PARAMETER Configuration 编译配置（Release / Debug）。
#>
function Invoke-Build {
    param(
        [string]$VersionDir,
        [string]$Configuration
    )

    $csproj = Join-Path $RepoRoot "$VersionDir\Source\$AssemblyName.csproj"
    if (-not (Test-Path $csproj)) { throw "未找到工程文件：$csproj" }

    Write-Step "编译 $VersionDir / $Configuration 配置"
    & dotnet build $csproj -c $Configuration -v minimal
    if ($LASTEXITCODE -ne 0) { throw "$VersionDir $Configuration 编译失败（exit $LASTEXITCODE）" }
}

<#
.SYNOPSIS 按排除清单递归复制一个白名单目录进发布包。
.DESCRIPTION
    不用「Copy-Item 整目录复制 + 事后 Remove-Item」的两段式：`<版本>\Source\obj` 下的
    中间产物会被 MSBuild 与 IDE 的 C# 语言服务（OmniSharp）读写，复制后立刻删除会与
    这些进程争抢文件句柄，偶发「正由另一进程使用」。改由 robocopy 在复制阶段就用 /XD
    排除，从根上不产生待删文件。
    ExcludeRelDirs 以包根为基准（如 `1.6\Source`），本函数只取落在本次复制目录内的部分
    （`Source`）作为排除项；若排除项等于本次目录本身，则整个目录不复制。
#>
function Copy-DirectoryExcluding {
    param(
        [string]   $SourcePath,
        [string]   $DestPath,
        [string]   $RelRoot,
        [string[]] $ExcludeRelDirs
    )

    # 把「相对包根」的排除项折算成 robocopy /XD 可用的相对路径
    $xd = @()
    foreach ($rel in $ExcludeRelDirs) {
        $normRel  = $rel     -replace '/', '\'
        $normRoot = $RelRoot -replace '/', '\'
        if ($normRel -ieq $normRoot) { return }
        $prefix = "$normRoot\"
        if ($normRel.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $xd += $normRel.Substring($prefix.Length)
        }
    }

    New-Item -ItemType Directory -Path $DestPath -Force | Out-Null

    # /R /W 显式调小：目标 dll 可能被运行中的游戏占用，robocopy 默认重试次数会长时间卡住脚本
    $robocopyArgs = @($SourcePath, $DestPath, '/E', '/NFL', '/NDL', '/NJH', '/NJS', '/NP', '/R:2', '/W:1')
    if ($xd.Count -gt 0) { $robocopyArgs += '/XD'; $robocopyArgs += $xd }

    $null = & robocopy @robocopyArgs
    # robocopy 退出码 0-7 为成功（含「跳过若干文件」），>=8 才是真失败
    if ($LASTEXITCODE -ge 8) {
        throw "复制目录失败（robocopy exit $LASTEXITCODE）：$SourcePath -> $DestPath"
    }
}

<#
.SYNOPSIS 按白名单组装单个发布包目录；Debug 包改写包内版本号。
.PARAMETER DllByVersion 哈希表：游戏版本目录（如 1.6）→ 该版本编译产物 dll 全路径。
#>
function Copy-PackageLayout {
    param(
        [string]$ModRoot,
        [hashtable]$DllByVersion,
        [bool]$IsDebug,
        [string]$Version
    )

    foreach ($pair in $DllByVersion.GetEnumerator()) {
        if (-not (Test-Path $pair.Value)) {
            throw "未找到编译产物：$($pair.Value)（请确认 $($pair.Key) 工程已按配置隔离输出目录）"
        }
    }

    if (Test-Path $ModRoot) { Remove-Item $ModRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $ModRoot -Force | Out-Null

    # 白名单目录复制：必需目录缺失报错，可选目录缺失跳过
    foreach ($dir in $IncludeDirs) {
        $src = Join-Path $RepoRoot $dir
        if (-not (Test-Path $src)) {
            if ($RequiredDirs -contains $dir -or $OptionalDirs -notcontains $dir) {
                throw "发布包必需目录缺失：$dir（如该 mod 确无此目录，请从 IncludeDirs/RequiredDirs 中移除）"
            }
            Write-Warning "目录不存在，已跳过：$dir"
            continue
        }
        Copy-DirectoryExcluding -SourcePath $src -DestPath (Join-Path $ModRoot $dir) `
            -RelRoot $dir -ExcludeRelDirs $ExcludeRelativeDirs
    }

    # 根级必需文件（如 LoadFolders.xml）
    foreach ($file in $IncludeRootFiles) {
        $src = Join-Path $RepoRoot $file
        if (-not (Test-Path $src)) { throw "发布包必需文件缺失：$file" }
        Copy-Item -Path $src -Destination (Join-Path $ModRoot $file) -Force
    }

    # 兜底剔除开发目录（复制阶段已排除，正常不会命中；命中即说明排除配置失效）
    foreach ($rel in $ExcludeRelativeDirs) {
        $victim = Join-Path $ModRoot $rel
        if (Test-Path $victim) {
            Remove-Item $victim -Recurse -Force
            Write-Host "    已剔除开发目录：$rel" -ForegroundColor DarkGray
        }
    }

    # 剔除指定文件模式（如本机后处理产物 .dds），保证包内容严格等于 commit 内容
    foreach ($pattern in $ExcludeFilePatterns) {
        $victims = @(Get-ChildItem -Path $ModRoot -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue)
        if ($victims.Count -gt 0) {
            $victims | Remove-Item -Force
            Write-Host "    已剔除文件（$pattern）：$($victims.Count) 个" -ForegroundColor DarkGray
            # 自检：剔除后不应再有匹配文件残留
            $residual = @(Get-ChildItem -Path $ModRoot -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue)
            if ($residual.Count -gt 0) {
                throw "组装结果异常：包内仍存在 $pattern（残留 $($residual.Count) 个）"
            }
        }
    }

    # 编译产物按版本分别放入 <版本>\Assemblies（LoadFolders.xml 只加载当前游戏版本对应目录）
    foreach ($pair in $DllByVersion.GetEnumerator()) {
        $asmDir = Join-Path $ModRoot "$($pair.Key)\Assemblies"
        New-Item -ItemType Directory -Path $asmDir -Force | Out-Null
        Copy-Item -Path $pair.Value -Destination $asmDir -Force
    }

    # 调试包：改写包内版本号，便于玩家在游戏内 mod 列表区分（不改动仓库原文件）
    if ($IsDebug) {
        $targetAbout = Join-Path $ModRoot 'About\About.xml'
        $text = [System.IO.File]::ReadAllText($targetAbout)
        $text = [regex]::Replace($text, '<modVersion>.*?</modVersion>', "<modVersion>$Version-$DebugModSuffix</modVersion>")
        [System.IO.File]::WriteAllText($targetAbout, $text, (New-Object System.Text.UTF8Encoding $true))
    }
}

<#
.SYNOPSIS 将目录压缩为 zip（保留顶层文件夹，显式 UTF-8 条目名以兼容中文文件名）。
#>
function New-ZipFromDirectory {
    param([string]$SourceDir, [string]$DestZip)

    Add-Type -AssemblyName System.IO.Compression | Out-Null

    if (Test-Path $DestZip) { Remove-Item $DestZip -Force }
    New-Item -ItemType Directory -Path (Split-Path -Parent $DestZip) -Force | Out-Null

    $baseDir = Split-Path -Parent $SourceDir
    $fileStream = [System.IO.File]::Open($DestZip, [System.IO.FileMode]::Create)
    $archive = New-Object -TypeName System.IO.Compression.ZipArchive -ArgumentList $fileStream, ([System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -Path $SourceDir -Recurse -File) {
            $entryName = $file.FullName.Substring($baseDir.Length + 1).Replace('\', '/')
            $entry = $archive.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
            $entryStream = $entry.Open()
            try {
                $inStream = [System.IO.File]::OpenRead($file.FullName)
                try { $inStream.CopyTo($entryStream) } finally { $inStream.Dispose() }
            } finally {
                $entryStream.Dispose()
            }
        }
    } finally {
        $archive.Dispose()
        $fileStream.Dispose()
    }
}

# ================== 主流程 ==================

$version = Get-ModVersion -Path $AboutPath
Write-Step "版本号：$version"

# 工作区未提交改动会导致包内容不对应任何 commit，仅告警
$dirty = & git -C $RepoRoot status --porcelain
if ($dirty) {
    Write-Warning "工作区存在未提交改动，Debug/Release 包可能不对应任何 commit：`n$dirty"
}

# 同名 tag 已存在通常意味着忘记提升版本号
$existingTag = & git -C $RepoRoot tag -l "v$version"
if ($existingTag) {
    Write-Warning "tag v$version 已存在，请确认是否已提升 <modVersion>"
}

# 仅对 $BuildVersionDirs 从源码编译 Release / Debug（同一源码、同一次运行产出，保证双包同源）；
# 其余版本使用仓库内库存 dll（冻结成熟版，见配置区注释）
foreach ($v in $BuildVersionDirs) {
    Invoke-Build -VersionDir $v -Configuration 'Release'
    Invoke-Build -VersionDir $v -Configuration 'Debug'
}

if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
New-Item -ItemType Directory -Path $StageDir -Force | Out-Null

# 版本目录 → 各配置产物 dll 路径（由 csproj 输出隔离约定保证）
$releaseDlls = @{}
$debugDlls   = @{}
foreach ($v in $GameVersionDirs) {
    $releaseDlls[$v] = Join-Path $RepoRoot "$v\Assemblies\$AssemblyName.dll"
    $debugDlls[$v]   = Join-Path $RepoRoot "$v\AssembliesDebug\$AssemblyName.dll"
}

# 冻结版本：库存 dll 存在性硬校验 + 打印时间戳（防止把忘记更新的旧 dll 发出去）
foreach ($v in $GameVersionDirs) {
    if ($v -in $BuildVersionDirs) { continue }
    Write-Step "版本 $v 为冻结成熟版，使用仓库库存 dll（未重新编译）"
    foreach ($dll in @($releaseDlls[$v], $debugDlls[$v])) {
        if (-not (Test-Path $dll)) { throw "冻结版本 $v 缺少库存 dll：$dll" }
        $item = Get-Item $dll
        Write-Host "    $($item.FullName.Replace($RepoRoot + '\',''))  编译时间 $($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))  $($item.Length) 字节"
    }
}

$releaseModRoot = Join-Path $StageDir "release\$PackageName"
$debugModRoot   = Join-Path $StageDir "$DebugModSuffix\$PackageName"

Copy-PackageLayout -ModRoot $releaseModRoot `
    -DllByVersion $releaseDlls `
    -IsDebug $false -Version $version

Copy-PackageLayout -ModRoot $debugModRoot `
    -DllByVersion $debugDlls `
    -IsDebug $true -Version $version

$releaseZip = Join-Path $DistDir "$PackageName-$version.zip"
$debugZip   = Join-Path $DistDir "$PackageName-$version-$DebugModSuffix.zip"

New-ZipFromDirectory -SourceDir $releaseModRoot -DestZip $releaseZip
New-ZipFromDirectory -SourceDir $debugModRoot -DestZip $debugZip

Write-Step "打包完成"
Write-Host "  Release 包：$releaseZip"
Write-Host "  Debug   包：$debugZip"
Write-Host "  组装目录  ：$StageDir"

if ($CreateRelease) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Warning "未检测到 gh CLI，跳过自动发布。请手动在 GitHub 创建 Release 并上传上述两个 zip。"
    } else {
        Write-Step "创建 GitHub Release v$version（请确认代码已 push，否则 tag 会指向远程旧 commit）"
        & gh release create "v$version" $releaseZip $debugZip --title "v$version" --generate-notes
        if ($LASTEXITCODE -ne 0) { throw "gh release create 失败（exit $LASTEXITCODE）" }
    }
}
