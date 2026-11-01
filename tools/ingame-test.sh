#!/usr/bin/env bash
# Builds this branch, installs exactly that build into Zeepkist, starts the game through Steam, lets the in-game test
# plugin walk it into the level editor and run the scenarios there, collects the results and puts the install, the
# mod's data and the levels the game saved meanwhile back as they were.
#
# Usage: tools/ingame-test.sh [--no-build] [--keep] [--timeout <seconds>] [<scenario.zlstest>...]
#   --no-build   use the existing Release build
#   --keep       leave the build installed for manual play (other copies of the mod stay set aside)
#   --timeout    give up and kill the game after this many seconds (default 300)
#   scenarios    run in the order given (default: the test plugin's Scenarios/smoke.zlstest)
#
# ZEEPKIST_DIR overrides where the game is installed. Results go to .ingame-test/<run id>/.
set -euo pipefail

app_id=1440670
mod_guid=Zeepkist.LogicScripting
test_guid=Zeepkist.LogicScripting.InGameTests
build=1
keep=0
timeout=300
scenarios=()

while (($#)); do
    case "$1" in
        --no-build) build=0 ;;
        --keep) keep=1 ;;
        --timeout)
            timeout="${2:?--timeout needs a number of seconds}"
            shift
            ;;
        -h | --help)
            sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        -*)
            echo "Unknown argument: $1" >&2
            exit 2
            ;;
        *) scenarios+=("$1") ;;
    esac
    shift
done

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
((${#scenarios[@]})) || scenarios=("$repo/$test_guid/Scenarios/smoke.zlstest")

log() { printf '[ingame-test] %s\n' "$*"; }
fail() {
    printf '[ingame-test] %s\n' "$*" >&2
    exit 1
}

# The test plugin finds each scenario by its file name, which also names it in the results
declare -A scenario_names=()
for scenario in "${scenarios[@]}"; do
    [[ -f "$scenario" ]] || fail "No scenario at $scenario"
    [[ "$scenario" == *.zlstest ]] || fail "$scenario isn't a .zlstest file"
    name="$(basename "$scenario")"
    [[ -z "${scenario_names[$name]:-}" ]] || fail "Two scenarios are called $name. Rename one."
    scenario_names[$name]=1
done

# The game itself, or Steam's launcher for it while it starts up or shuts down. Matching on names rather than any
# command line that mentions the game keeps other shells out of it.
game_running() {
    pgrep -x Zeepkist.exe >/dev/null || pgrep -f "^\\S*/reaper SteamLaunch AppId=$app_id " >/dev/null
}

# Parallel worktrees share one game, so a second run queues here until the first is done
lock="${XDG_RUNTIME_DIR:-/tmp}/zls-ingame-test.lock"
exec 9>"$lock"
if ! flock -n 9; then
    log "Another in-game test run is going, waiting for it to finish"
    flock 9
fi

game_running && fail "Zeepkist is already running. Close it first."
pgrep -x steam >/dev/null || fail "Steam isn't running. Start Steam first."

if ((build)); then
    log "Building"
    # The build leaves compiler servers running, which mustn't inherit the lock and keep later runs waiting
    dotnet build "$repo/Zeepkist.LogicScripting/Zeepkist.LogicScripting.csproj" -c Release -v quiet -nologo 9>&-
    dotnet build "$repo/$test_guid/$test_guid.csproj" -c Release -v quiet -nologo 9>&-
fi

mod_build="$repo/Zeepkist.LogicScripting/bin/Release/net472"
test_build="$repo/$test_guid/bin/Release/net472"
[[ -f "$mod_build/$mod_guid.dll" && -f "$test_build/$test_guid.dll" ]] || fail "No Release build found. Run without --no-build."

find_game() {
    if [[ -n "${ZEEPKIST_DIR:-}" ]]; then
        echo "$ZEEPKIST_DIR"
        return
    fi
    local steam_root library
    for steam_root in "$HOME/.local/share/Steam" "$HOME/.steam/steam"; do
        [[ -f "$steam_root/steamapps/libraryfolders.vdf" ]] || continue
        while read -r library; do
            if [[ -f "$library/steamapps/appmanifest_$app_id.acf" ]]; then
                echo "$library/steamapps/common/Zeepkist"
                return
            fi
        done < <(sed -n 's/^\s*"path"\s*"\(.*\)"/\1/p' "$steam_root/steamapps/libraryfolders.vdf")
    done
}

game="$(find_game)"
[[ -n "$game" && -f "$game/Zeepkist.exe" ]] || fail "Couldn't find Zeepkist. Set ZEEPKIST_DIR to its install folder."
bepinex="$game/BepInEx"
plugins="$bepinex/plugins"
[[ -d "$plugins" ]] || fail "BepInEx isn't installed in $game"

prefix="$(cd "$game/../.." && pwd)/compatdata/$app_id/pfx"
[[ -d "$prefix" ]] || fail "No Proton prefix at $prefix, so the mod's data can't be backed up. Is $game in a Steam library?"
game_data="$prefix/drive_c/users/steamuser/AppData/Roaming/Zeepkist"
mod_data="$game_data/Mods/$mod_guid"
# Test mode and leaving the editor save the level into these, on top of the player's own
level_saves=(Autosaves Backups)
mod_cfg="$bepinex/config/$mod_guid.cfg"
test_cfg="$bepinex/config/$test_guid.cfg"
disabled="$bepinex/zls-ingame-test-disabled"
deploy="$plugins/Sideloaded/Plugins/$mod_guid"
test_deploy="$plugins/Sideloaded/Plugins/$test_guid"
build_files=("$mod_guid.dll" "$mod_guid.pdb" "$mod_guid.Compiler.dll" "$mod_guid.Compiler.pdb")
# Marks a build --keep left installed, so the next run removes it instead of taking it for the player's own copy
kept_marker="$deploy/.zls-ingame-test-build"

run_id="$(date +%Y%m%d-%H%M%S)"
run_dir="$repo/.ingame-test/$run_id"
backup="$run_dir/backup"
mkdir -p "$backup"
log "Run $run_id, game in $game"

launched=0
restored=0
deployed=0
timed_out=0
interrupted=0
passed=false

stop_game() {
    game_running || return 0
    log "Stopping the game"
    pkill -x Zeepkist.exe || true
    for _ in $(seq 30); do
        game_running || return 0
        sleep 1
    done
    pkill -9 -x Zeepkist.exe || true
    pkill -f "^\\S*/reaper SteamLaunch AppId=$app_id " || true
    sleep 2
}

# Moves every file under $1 back to the same path under $2, keeping whatever is already there
move_tree_back() {
    local from="$1" to="$2" file relative
    [[ -d "$from" ]] || return 0
    while IFS= read -r -d '' file; do
        relative="${file#"$from"/}"
        mkdir -p "$(dirname "$to/$relative")"
        if [[ -e "$to/$relative" ]]; then
            log "Not moving back $relative: something is already at $to/$relative"
        else
            mv "$file" "$to/$relative"
        fi
    done < <(find "$from" -type f -print0)
    find "$from" -depth -type d -empty -delete
}

remove_build() {
    local file
    for file in "${build_files[@]}"; do
        rm -f "$deploy/$file"
    done
    rm -f "$kept_marker"
    [[ -d "$deploy" ]] && rmdir --ignore-fail-on-non-empty "$deploy"
    return 0
}

# The exceptions the game logged after the test plugin finished the run, so as it quit, from its copied log. Each is
# Unity's log line, then "Stack trace:" and the frames: they're grouped by the line and the first frame, with a count.
# The game can quit before the log is all written, so a frame cut off at the end counts under the frame it starts.
quit_exceptions() {
    [[ -f "$run_dir/LogOutput.log" ]] || return 0
    local cut=0
    [[ -n "$(tail -c 1 "$run_dir/LogOutput.log")" ]] && cut=1
    awk -v finished="[ZLS-TEST] run $run_id " -v cut="$cut" '
        function count(message, frame,    key) {
            key = message "\t" frame
            if (!(key in counts)) order[++keys] = key
            counts[key]++
        }
        # Held back a line, so the last one can be told apart
        function record(frame) {
            if (pending) count(pending_message, pending_frame)
            pending = 1
            pending_message = message
            pending_frame = frame
            pending_line = NR
            state = 0
        }
        { sub(/\r$/, "") }
        !quitting {
            if (index($0, finished) && / (passed|failed)$/) quitting = 1
            next
        }
        state == 2 && !/^\[/ { record($0); next }
        state == 2 { record("") }
        state == 1 && $0 == "Stack trace:" { state = 2; next }
        /^\[/ { state = 0 }
        /^\[Error *: *Unity Log\] / { message = $0; sub(/^\[[^]]*\] /, "", message); state = 1 }
        END {
            print ""
            print "## Exceptions at quit"
            print ""
            if (!quitting) {
                print "Not read: the run didn\047t finish."
                exit
            }
            if (state == 2) record("")
            if (pending && cut && pending_line == NR && pending_frame != "") {
                for (i = 1; i <= keys; i++) {
                    split(order[i], parts, "\t")
                    if (parts[1] == pending_message && index(parts[2], pending_frame) == 1) {
                        pending_frame = parts[2]
                        break
                    }
                }
                if (i > keys) pending_frame = pending_frame "…"
            }
            if (pending) count(pending_message, pending_frame)
            if (keys == 0) print "None."
            for (i = 1; i <= keys; i++) {
                split(order[i], parts, "\t")
                printf "- %s`%s`%s\n", (counts[order[i]] > 1 ? counts[order[i]] "× " : ""), parts[1], (parts[2] != "" ? " at `" parts[2] "`" : "")
            }
        }
    ' "$run_dir/LogOutput.log"
}

# Copies what the run left in the game folder into the run folder, so an interrupted or failed run has results too
collect() {
    [[ -d "$test_deploy/out" ]] && cp -a "$test_deploy/out/." "$run_dir/"
    ((launched)) && cp "$bepinex/LogOutput.log" "$run_dir/LogOutput.log"

    passed=false
    if [[ -f "$run_dir/results.json" ]]; then
        passed="$(jq -r '.Passed' "$run_dir/results.json")"
        if ((timed_out)); then
            passed=false
            jq '.TimedOut = true | .Passed = false' "$run_dir/results.json" >"$run_dir/results.json.tmp"
            mv "$run_dir/results.json.tmp" "$run_dir/results.json"
        fi
    fi

    {
        echo "# In-game test run $run_id"
        echo
        if ((interrupted)); then
            echo "**Interrupted.**"
        elif ((timed_out)); then
            echo "**Timed out** after $timeout seconds."
        elif [[ "$passed" == true ]]; then
            echo "**Passed.**"
        else
            echo "**Failed.**"
        fi
        echo
        if [[ -f "$run_dir/results.json" ]]; then
            # The walk to the editor first, then each scenario with its steps, which stop at the first that failed, and
            # the exceptions the game logged meanwhile
            jq -r '
                def line: "- \(if .Passed then "PASS" else "FAIL" end) \(if .Line > 0 then "line \(.Line): " else "" end)\(.Name)"
                    + (if .Detail != "" then ": \(.Detail)" else "" end)
                    + (if .Expected != null and (.Passed | not) then "\n  - expected: `\(.Expected)`\n  - actual: `\(.Actual)`" else "" end);
                (.Exceptions // []) as $exceptions
                | def exceptions($scenario): $exceptions | map(select(.Scenario == $scenario))
                    | if length == 0 then empty else "", "Exceptions logged:", "",
                        (.[] | "- \(if .Count > 1 then "\(.Count)× " else "" end)`\(.Message)`" + (if .Frame != "" then " at `\(.Frame)`" else "" end))
                      end;
                  (.Steps | map(select(.Scenario == "")) | map(line) | .[]),
                  exceptions(""),
                  (.Steps | map(select(.Scenario != "")) | group_by(.Scenario) | sort_by(.[0].Index)[]
                    | "", "## \(.[0].Scenario): \(if all(.Passed) then "passed" else map(select(.Passed | not))[0] | "failed\(if .Line > 0 then " at line \(.Line)" else "" end)" end)", "",
                      (.[] | line), exceptions(.[0].Scenario))
            ' <(jq '.Steps |= to_entries | .Steps |= map(.value + {Index: .key})' "$run_dir/results.json")
            quit_exceptions
            echo
            jq -r '.Screenshots[] | "- ![\(.)](\(.))"' "$run_dir/results.json"
        else
            echo "No results: the test plugin didn't write any. See LogOutput.log."
        fi
    } >"$run_dir/results.md"
}

# Runs once, from the end of a run or the EXIT trap. It mustn't stop halfway, so a second Ctrl+C is ignored and a
# failing step is logged instead of ending the script.
restore() {
    ((restored)) && return
    restored=1
    trap '' INT TERM
    set +e
    ((launched)) && stop_game
    collect

    rm -rf "$test_deploy"
    [[ -f "$backup/had-test-cfg" ]] || rm -f "$test_cfg"

    # The mod saves its data when the game quits, so this has to wait until the game is gone
    if [[ -f "$backup/had-mod-data" ]]; then
        rm -rf "$mod_data"
        cp -a "$backup/mod-data" "$mod_data"
    elif [[ -f "$backup/backed-up" ]]; then
        rm -rf "$mod_data"
    fi
    for folder in "${level_saves[@]}"; do
        if [[ -f "$backup/had-$folder" ]]; then
            rm -rf "${game_data:?}/$folder"
            cp -a "$backup/$folder" "$game_data/$folder"
        elif [[ -f "$backup/backed-up" ]]; then
            rm -rf "${game_data:?}/$folder"
        fi
    done
    if [[ -f "$backup/had-mod-cfg" ]]; then
        cp -a "$backup/$mod_guid.cfg" "$mod_cfg"
    elif [[ -f "$backup/backed-up" ]]; then
        rm -f "$mod_cfg"
    fi

    if ((keep && deployed)); then
        touch "$kept_marker"
        log "Kept the test build in $deploy"
        [[ -d "$disabled" ]] && log "Other copies of the mod are set aside in $disabled, the next run puts them back"
    else
        ((deployed)) && remove_build
        move_tree_back "$disabled" "$plugins"
        log "Restored the game install"
    fi
    log "Restored the mod's data and the game's level saves"
    set -e
}

on_exit() {
    local code=$?
    restore
    exit "$code"
}
trap on_exit EXIT
trap 'interrupted=1; log "Interrupted"; exit 130' INT TERM

# A run with --keep, or one that was killed outright, may have left its build installed and other copies set aside:
# undo that first, so this run sets aside, and afterwards restores, the install as it really is
if [[ -f "$kept_marker" ]]; then
    log "Removing the build an earlier --keep run left installed"
    remove_build
fi
if [[ -d "$disabled" ]]; then
    log "Putting back copies of the mod an earlier run set aside"
    move_tree_back "$disabled" "$plugins"
fi
rm -rf "$test_deploy"

if [[ -d "$mod_data" ]]; then
    cp -a "$mod_data" "$backup/mod-data"
    touch "$backup/had-mod-data"
fi
if [[ -f "$mod_cfg" ]]; then
    cp -a "$mod_cfg" "$backup/$mod_guid.cfg"
    touch "$backup/had-mod-cfg"
fi
[[ -f "$test_cfg" ]] && touch "$backup/had-test-cfg"
for folder in "${level_saves[@]}"; do
    if [[ -d "$game_data/$folder" ]]; then
        cp -a "$game_data/$folder" "$backup/$folder"
        touch "$backup/had-$folder"
    fi
done
touch "$backup/backed-up"

while IFS= read -r -d '' file; do
    relative="${file#"$plugins"/}"
    mkdir -p "$(dirname "$disabled/$relative")"
    mv "$file" "$disabled/$relative"
    log "Set aside $relative"
done < <(find "$plugins" -type f \( -iname "$mod_guid*" -o -iname 'net.tnrd.zeepkist.logicscripting*' \) -print0)

mkdir -p "$deploy" "$test_deploy"
deployed=1
for file in "${build_files[@]}"; do
    if [[ -f "$mod_build/$file" ]]; then
        cp "$mod_build/$file" "$deploy/"
    fi
done
cp "$test_build/$test_guid.dll" "$test_build/$test_guid.pdb" "$test_deploy/"
mkdir -p "$test_deploy/scenarios"
scenario_files=()
for scenario in "${scenarios[@]}"; do
    cp "$scenario" "$test_deploy/scenarios/"
    scenario_files+=("$(basename "$scenario")")
done
jq -n --arg runId "$run_id" '{runId: $runId, scenarios: $ARGS.positional}' --args "${scenario_files[@]}" >"$test_deploy/run.json"

log "Starting the game"
launched=1
# Closing the lock for it too, as a Steam process can outlive this script
steam -applaunch "$app_id" -zls-ingame-test "$run_id" >/dev/null 2>&1 9>&- &
disown

results="$test_deploy/out/results.json"
deadline=$((SECONDS + timeout))
started=0
while :; do
    if [[ -f "$results" ]] && [[ "$(jq -r '.Done' "$results" 2>/dev/null)" == true ]]; then
        break
    fi
    if game_running; then
        started=1
    elif ((started)); then
        log "The game quit before the run finished"
        break
    fi
    if ((SECONDS > deadline)); then
        log "Timed out after $timeout seconds"
        timed_out=1
        break
    fi
    sleep 1
done

# Let a finished run quit by itself, so the mod saves its data the way it does for a player
if ((!timed_out)); then
    for _ in $(seq 30); do
        game_running || break
        sleep 1
    done
fi
stop_game

restore
echo
grep -E '^- (PASS|FAIL)|^  - |^## |^\*\*|^No results|^Exceptions logged|^- ([0-9]+× )?`|^None\.$|^Not read' "$run_dir/results.md" | sed 's/\*\*//g; s/`//g'
log "Results in $run_dir"
if [[ "$passed" == true ]]; then
    log "Passed: $(jq -r '[.Steps[].Scenario | select(. != "")] | unique | join(", ")' "$run_dir/results.json")"
    exit 0
fi
exit 1
