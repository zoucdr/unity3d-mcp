# Fix all "as Type()" patterns - should be "as Type"
Write-Host "Fixing 'as Type()' patterns..." -ForegroundColor Green

$files = Get-ChildItem -Path "Packages/unity-package/Editor" -Recurse -Filter "*.cs"
$fixCount = 0

foreach ($file in $files)
{
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $original = $content
    
    # Fix: as Type() -> as Type
    # Match pattern: as followed by Type name and ()
    $content = $content -replace ' as (\w+(?:\.\w+)*)\(\)', ' as $1'
    $content = $content -replace ' as (\w+(?:\.\w+)*)\?\(\)', ' as $1'
    
    if ($content -ne $original)
    {
        $fixCount++
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Cyan
    }
}

Write-Host "`nFixed $fixCount files" -ForegroundColor Green
