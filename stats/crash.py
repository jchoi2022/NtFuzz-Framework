import os, sys, shutil
from time import mktime, strftime
from datetime import datetime
from triage import Classification, ClassifyError
from cdb import parse_log

class CrashInfo:
    def __init__(self, iter_nth, timestamp, classification):
        self.iter_nth = iter_nth
        self.timestamp = timestamp
        self.classification = classification

def calc_timestamp(rounds, logfile):
    crash_time = os.path.getmtime(logfile)
    accum_time = 0.0
    for (s, e) in rounds:
        start_time = mktime(datetime.strptime(s, "%Y-%m-%d-%H-%M").timetuple())
        end_time = mktime(datetime.strptime(e, "%Y-%m-%d-%H-%M").timetuple())
        if start_time <= crash_time and crash_time <= end_time:
            return int(accum_time + crash_time - start_time)
        accum_time += (end_time - start_time)
    time_str = datetime.fromtimestamp(crash_time).strftime("%Y-%m-%d-%H-%M")
    print "Failed to identify belonging round of %s (%s)" % (logfile, time_str)
    exit(1)

def analyze_cdb_log(rounds, cdb_logfile, iter_nth, crashes):
    timestamp = calc_timestamp(rounds, cdb_logfile)

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

    if classification.stack_invalid:
        return

    crash_info = CrashInfo(iter_nth, timestamp, classification)
    crashes.append(crash_info)

def get_round_info(tasks_dir):
    time_file = os.path.join(tasks_dir, "time.txt")
    f = open(time_file, "r")
    rounds = []
    for line in f:
        start = line.strip().split()[0]
        end = line.strip().split()[1]
        rounds.append( (start, end) )
    f.close()
    return rounds

def process_crashes(tasks_dir, iteration, crashes):
    rounds = get_round_info(tasks_dir)
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
                analyze_cdb_log(rounds, filepath, iter_nth, crashes)

def plot_total_crashes(crashes, hour, iteration, delta):
    crash_count_dict = { }
    for i in range(iteration):
        crash_count_dict[i] = 0
    for h in range(0, hour + delta, delta):
        sec = 3600 * h
        cnt_list = []
        for i in range(iteration):
            crash_count = crash_count_dict[i]
            for crash in crashes:
                time, iter_nth = crash.timestamp, crash.iter_nth
                if time < sec and iter_nth == i:
                    crash_count += 1
            cnt_list.append(crash_count)
        cnt_avg = float(sum(cnt_list)) / float(iteration)
        cnt_str = ", ".join(list(map(str, cnt_list)))
        print "Avg total crash @ hour %d = %.2f [%s]" % (h, cnt_avg, cnt_str)
    print "========================="

def plot_unique_crashes(crashes, hour, iteration, delta):
    hashes_dict = { }
    for i in range(iteration):
        hashes_dict[i] = []
    for h in range(0, hour + delta, delta):
        sec = 3600 * h
        cnt_list = []
        for i in range(iteration):
            hashes = hashes_dict[i]
            for crash in crashes:
                time, iter_nth = crash.timestamp, crash.iter_nth
                hash = crash.classification.hash
                if time < sec and hash not in hashes and iter_nth == i:
                    hashes.append(hash)
            cnt_list.append(len(hashes))
        cnt_avg = float(sum(cnt_list)) / float(iteration)
        cnt_str = ", ".join(list(map(str, cnt_list)))
        print "Avg unique crash @ hour %d = %.2f [%s]" % (h, cnt_avg, cnt_str)
    print "========================="

def print_stack_hashes(crashes):
    hashes = []
    for crash in crashes:
        if crash.classification.hash not in hashes:
            hashes.append(crash.classification.hash)
    hashes.sort()
    for i in range(len(hashes)):
        print "Crash #%d: %s" % (i, hashes[i])
        print "------------"
    print "========================="

if len(sys.argv) != 4 and len(sys.argv) != 5:
    print("Usage: python %s <dir> <hour> <iter> <delta(opt)>" % sys.argv[0])
    exit(1)

tasks_dir = sys.argv[1]
hour = int(sys.argv[2])
iteration = int(sys.argv[3])
crashes = []
process_crashes(tasks_dir, iteration, crashes)
if len(sys.argv) == 5:
    delta = int(sys.argv[4])
else:
    delta = 1

print_stack_hashes(crashes)
plot_total_crashes(crashes, hour, iteration, delta)
plot_unique_crashes(crashes, hour, iteration, delta)
