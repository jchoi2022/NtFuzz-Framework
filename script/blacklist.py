import sys

FLAG_SIG = '"debug":'

def usage():
    print "Usage : python %s <Type JSON> <syscall> <crash | leak>" % sys.argv[0]
    exit(1)

if len(sys.argv) != 4:
    usage()

type_file = sys.argv[1]
syscall = sys.argv[2]
if sys.argv[3] == "crash":
    is_leak = False
elif sys.argv[3] == "leak":
    is_leak = True
else:
    usage()

f = open(type_file, "r")
buf = f.read()
f.close()

syscall_idx = buf.find(syscall)
if syscall_idx == -1:
    print "Syscall %s not found from file" % syscall
    exit(1)

start_idx = syscall_idx + len(syscall)
flag_idx = buf.find(FLAG_SIG, start_idx)
if flag_idx == -1:
    print "Flag signature not found from file"
    exit(1)

flag_idx = flag_idx + len(FLAG_SIG)
flag_end = buf.find(",", flag_idx)
flag = int(buf[flag_idx:flag_end], 10)

if is_leak:
    flag = flag | 8 # Bit 3 represents no-fuzzing flag.
else:
    flag = flag | 4 # Bit 2 represents no-check flag.

buf = buf[:flag_idx] + ("%d" % flag) + buf[flag_end:]

f = open(sys.argv[1], "w")
f.write(buf)
f.close()
