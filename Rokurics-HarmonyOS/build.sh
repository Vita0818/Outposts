#\!/bin/bash
# Rokurics HarmonyOS build script
# Usage: ./build.sh [clean]

export NODE_HOME=/Applications/DevEco-Studio.app/Contents/tools/node
export DEVECO_SDK_HOME=/Applications/DevEco-Studio.app/Contents/sdk
export JAVA_HOME=/Applications/DevEco-Studio.app/Contents/jbr/Contents/Home
export PATH="$JAVA_HOME/bin:$NODE_HOME/bin:$PATH"

HW="/Applications/DevEco-Studio.app/Contents/tools/hvigor/bin/hvigorw"

if [ "$1" = "clean" ]; then
    $HW --no-daemon clean 2>&1
    echo "--- Clean done, building fresh ---"
fi

$HW --no-daemon assembleHap --mode module -p module=entry@default 2>&1
