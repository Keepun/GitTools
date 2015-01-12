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

GitLineTrim
-----------------------------
Newline is determined by the first line of the file.<br/>
Full support for Unicode.
```sh
git config --global filter.linetrim.clean "\"path/GitLineTrim.exe\""
```
```sh
echo -e '*.ext\tfilter=linetrim' >> $HOME/.config/git/attributes
```
or
```sh
echo -e '*.ext\tfilter=linetrim' >> .gitattributes
```

GitUnicode
-----------------------------
Conversion from all versions of Unicode in UTF-8 for GNU Diff.
```sh
git config --global diff.unicode.textconv "\"path/GitUnicode.exe\""
```
```sh
echo -e '*.ext\tdiff=unicode' >> $HOME/.config/git/attributes
```
or
```sh
echo -e '*.ext\tdiff=unicode' >> .gitattributes
```
