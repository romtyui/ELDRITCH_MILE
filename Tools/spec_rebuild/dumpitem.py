import sys, re, io, glob, os

BS = chr(92)
UPAT = re.compile(BS + BS + r'u([0-9a-fA-F]{4})')


def unesc(s):
    s = UPAT.sub(lambda m: chr(int(m.group(1), 16)), s)
    return s.replace(BS + 'n', '\n      ')


out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')
for pat in sys.argv[1:]:
    for path in sorted(glob.glob(pat)):
        txt = open(path, encoding='utf-8').read()
        body = txt.split('m_EditorClassIdentifier:', 1)[-1]
        out.write("======== %s ========\n" % os.path.basename(path))
        out.write(unesc(body).strip() + "\n\n")
out.flush()
