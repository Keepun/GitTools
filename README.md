GitTools
=============================

GitLauncher
-----------------------------
[Fix #270 bug.](https://github.com/msysgit/git/issues/270)

GitEditor
-----------------------------
Starting the GUI-editor for commit, rebase, etc.
```sh
git config --global core.editor "\"path/GitEditor.exe\" \"path/Editor.exe\""
```

GitFilters
-----------------------------
I merged GitLineTrim and GitUnicode because of [a #303 bug in Git.](https://github.com/msysgit/git/issues/303)<br/>
Newline is determined by the first line of the file.<br/>
Full support for Unicode.
```
GitFilters [-h | -l] [options] [--from Codepage] [--to Codepage] [files]
  -h --help        Show this help
  -l --list        List support codepages
     --no-bom      Not to add BOM for Unicode
     --linetrim    line.TrimEnd()
     --addline     Add a line to the end of the file
     --from        Convert From codepages
     --to          Convert To codepages
     --no-backup   Do not create backup files
P.S.
When the input stream files are ignored.
Auto detection From only for Unicode with BOM.
If you do not set the From and not Unicode, then the conversion will not.
To=UTF-8 with BOM default.
To=None to disable the conversion.
```
An example of a filter for the files in UTF-16 (UCS-2):
```sh
git config --global filter.utf16.clean "\"path/GitFilters.exe\" --linetrim --addline"
git config --global filter.utf16.smudge "\"path/GitFilters.exe\" --to UTF-16"
```
```sh
echo -e '*.ext\tfilter=utf16' >> $HOME/.config/git/attributes
```
or
```sh
echo -e '*.ext\tfilter=utf16' >> .gitattributes
```
#### GitLineTrim
```sh
GitFilters.exe --to none --linetrim [...]
```
#### GitUnicode
```sh
GitFilters.exe --to UTF-16 [...]
```
