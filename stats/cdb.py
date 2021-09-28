
STACK_START_INDICATOR = "STACK_TEXT:"

def is_hexa_str(s):
    try:
        tmp = int(s, 16)
        return True
    except:
        return False

def filter_call_stack(stack):
    call_lines = stack.split("\n")
    filtered_calls = []
    for call_line in call_lines:
        # Remove whitespace for correct parsing.
        call_line = call_line.replace("vector deleting destructor",
                                      "vector_deleting_destructor")
        if call_line.strip() == "":
            continue
        call = call_line.split()[-1]
        # If it's error message line, then skip.
        if call == "wrong.": # "... Following frames may be wrong."
            continue
        if call == "file.": # "... too large to be in the dump file."
            continue
        if call == "details": # "... Type ".hh dbgerr004" for details"
            continue
        if call == "extended": # "... not properly sign extended"
            continue
        if call == "reliable.": # "...  may not be completely reliable."
            continue
        if "GetContextState failed" in call_line:
            continue
        if "Unable to get current machine context" in call_line:
            continue

        # We will ignore trap handler frame, since it's not true crash point.
        if "KiTrap" in call:
            filtered_calls = []
            continue

        # If we cannot identify to which module the address belongs to, ignore.
        # This happens when (1) the address belongs to an unknown user-space
        # module, or (2) kernel code jumped to an invalid address.
        if is_hexa_str(call):
            continue

        if "!" not in call and "+" not in call:
            print "Unexpected call stack line: %s" % call_line
            print stack
            exit(1)

        if "+" in call:
            call = call.split("+")[0]

        filtered_calls.append(call)

    filtered_stack = "\n".join(filtered_calls)
    return filtered_stack

def parse_log(cdb_logfile):
    f = open(cdb_logfile, "r")
    cdb_log = f.read()
    f.close()

    if STACK_START_INDICATOR not in cdb_log:
        print "No %s in cdb log %s" % (STACK_START_INDICATOR, cdb_logfile)
        return None

    start_idx = cdb_log.find(STACK_START_INDICATOR)
    end_idx = cdb_log.find("\r\n\r\n", start_idx)
    stack = cdb_log[start_idx + len(STACK_START_INDICATOR):end_idx]
    stack = filter_call_stack(stack)

    return stack


