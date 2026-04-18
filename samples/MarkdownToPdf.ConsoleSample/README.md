# MarkdownToPdf.ConsoleSample

This sample generates `sample.pdf` using TrueType fonts provided by path.

Usage (one font for all styles):
```powershell
dotnet run --project samples/MarkdownToPdf.ConsoleSample/MarkdownToPdf.ConsoleSample.csproj -- "C:\Windows\Fonts\arial.ttf"
```

Usage (four fonts: regular, bold, italic, monospace):
```powershell
dotnet run --project samples/MarkdownToPdf.ConsoleSample/MarkdownToPdf.ConsoleSample.csproj -- "C:\Windows\Fonts\arial.ttf" "C:\Windows\Fonts\arialbd.ttf" "C:\Windows\Fonts\ariali.ttf" "C:\Windows\Fonts\cour.ttf"
```

Output is written to `sample.pdf` in the app output folder.
