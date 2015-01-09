GitLineTrim
===============================
Полная поддержка Unicode, но перевод строки зависит от ОСи.
Определение бинарика тоже есть.

$ git config --global filter.linetrim.clean "\"path/GitLineTrim.exe\" -nobackup %f"

$ echo -e '*.ext\tfilter=linetrim' >> $HOME/.config/git/attributes
или
$ echo -e '*.ext\tfilter=linetrim' >> .gitattributes
