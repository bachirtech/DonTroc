#!/bin/zsh
PID="$PID"
while ps -p "$PID" > /dev/null; do
  sleep 10
done
# when finished, capture final log tail and list .aab
echo "BUILD_FINISHED at $(date '+%Y-%m-%d %T')" > build_logs/build_aab.done
tail -n 500 build_logs/build_aab.log > build_logs/build_aab.final.log
find DonTroc -type f -name "*.aab" -maxdepth 6 -print > build_logs/aab_files.txt || true
