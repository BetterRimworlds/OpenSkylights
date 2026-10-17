#!/bin/bash
# ==== build.sh ====
# Check if inotifywait is installed
if [ -z "$(which inotifywait)" ]; then
    echo "inotifywait not installed."
    echo "Install the inotify-tools package and try again."
    exit 1
fi

# Directory to monitor for changes
dir="./Source"
MOD=$(basename $PWD)
if [[ ! -f "Source/${MOD}.sln" ]]; then
    MOD=$(basename "$(ls Source/*.sln | head -n1)" .sln)
fi

# Define the path to your solution file
solutionPath="Source/${MOD}.sln"

# 1.2–1.6 are net48 and share one restore. 1.7 is net10.0, so it restores
# on its own after those finish. A shared --no-restore assets file cannot
# hold both target frameworks.
net48_configurations=("v1.2" "v1.3" "v1.4" "v1.5" "v1.6")

dotnet restore "$solutionPath"

function sync_mod() {
    if [ -d "${MOD}/Languages/Spanish" ]; then
        rm -rf "${MOD}/Languages/SpanishLatin"
        cp -avf ${MOD}/Languages/Spanish ${MOD}/Languages/SpanishLatin
    fi

    # Copy over the mod directory.
    rsync -a ${MOD} /rimworld/1.2/Mods/

    # Copy over and reformat the README.
    cp README.md /rimworld/1.2/Mods/${MOD}
    unix2dos /rimworld/1.2/Mods/${MOD}/README.md

    local dest
    for dest in \
        /rimworld/1.3/Mods \
        /rimworld/1.4/Mods \
        /rimworld/1.5/Mods \
        /rimworld/1.6/Mods \
        /rimworld/1.6-steam/Mods \
        /rimworld/1.7-steam/Mods
    do
        if [ -d "$dest" ]; then
            rm -rf "$dest/${MOD}"
            cp -af "/rimworld/1.2/Mods/${MOD}" "$dest/"
        fi
    done
}

function build() {
    rm -rf /rimworld/1.2/Mods/${MOD}

    # Loop through each configuration and build it
    local pids=()
    for config in "${net48_configurations[@]}"; do
        echo "Building for configuration: $config"
        dotnet build --no-restore "$solutionPath" --configuration "Release $config" &
        pids+=($!)
    done

    local failed=0
    for pid in "${pids[@]}"; do
        wait "$pid" || { echo "Build failed (PID $pid)"; failed=1; }
    done

    if [[ $failed -eq 1 ]]; then
        echo "One or more builds failed. Aborting sync."
        return 1
    fi

    echo "Building for configuration: v1.7"
    dotnet build "$solutionPath" --configuration "Release v1.7" || {
        echo "1.7 build failed. Aborting sync."
        return 1
    }

    sync_mod
    echo "All builds completed!"
}

build || exit 1

if [ "$1" == "1" ]; then
    echo "Done"
    exit
fi

## Watch for changes to .cs and XML files in the directory and subdirectories
#inotifywait --recursive --monitor --format "%e %w%f" \
#    --exclude '/\.idea($|/)' \
#    --event modify,move,create,delete "$dir" "$MOD" |
#    while read event fullpath; do
#        if [[ "$fullpath" == "$dir"* && "$fullpath" == *.cs ]]; then
#            echo "Running build for $fullpath"
#            build || echo "Build failed, skipping sync."
#        elif [[ "$fullpath" == "$MOD"* && "$fullpath" == *.xml ]]; then
#            echo "Running sync_mod for $fullpath"
#            sync_mod
#        fi
#    done
