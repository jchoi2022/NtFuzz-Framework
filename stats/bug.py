import os, sys, shutil
from time import mktime, strftime
from datetime import datetime
from triage import Classification, ClassifyError
from cdb import parse_log

class CrashInfo:
    def __init__(self, iter_nth, classification):
        self.iter_nth = iter_nth
        self.classification = classification

def analyze_cdb_log(cdb_logfile, iter_nth, crashes):
    stack = parse_log(cdb_logfile)
    if stack is None or stack == "":
        return

    try:
        classification = Classification(stack)
    except ClassifyError as e:
        print stack
        print e.msg
        print "Failed to classify log file %s" % cdb_logfile
        exit(1)

    crash_info = CrashInfo(iter_nth, classification)
    crashes.append(crash_info)

def process_crashes(tasks_dir, iteration, crashes):
    for name in os.listdir(tasks_dir):
        if name == "time.txt":
            continue
        task_dir = os.path.join(tasks_dir, name)
        task_no = int(task_dir.split("-")[-1], 10)
        iter_nth = task_no % iteration
        crash_dir = os.path.join(task_dir, "crash")
        for filename in os.listdir(crash_dir):
            filepath = os.path.join(crash_dir, filename)
            if filepath.endswith(".cdb"):
                analyze_cdb_log(filepath, iter_nth, crashes)

def print_unique_bugs(crashes, iteration):
    bug_list_dict =  { }
    for i in range(iteration):
        bug_list_dict[i] = []

    for crash in crashes:
        iter_nth, bug_type = crash.iter_nth, crash.classification.main_type
        if bug_type not in bug_list_dict[iter_nth]:
            bug_list_dict[iter_nth].append(bug_type)

    for i in range(iteration):
        print "(Iteration %d)" % (i + 1)
        bug_list = bug_list_dict[i]
        bug_list.sort()
        for i in range(len(bug_list)):
            print "%s" % (bug_list[i])
        print "========================="

    total_set = []
    total_count = 0
    for i in range(iteration):
        total_count += len(bug_list_dict[i])
        for bug_type in bug_list_dict[i]:
            if bug_type not in total_set:
                total_set.append(bug_type)
    avg_count = float(total_count) / float(iteration)
    print "Average # of unique bugs: %.1f" % avg_count
    print "Total # of unique bugs: %d" % len(total_set)

if len(sys.argv) != 3:
    print("Usage: python %s <dir> <iter>" % sys.argv[0])
    exit(1)

tasks_dir = sys.argv[1]
iteration = int(sys.argv[2])
crashes = []
process_crashes(tasks_dir, iteration, crashes)

print_unique_bugs(crashes, iteration)
