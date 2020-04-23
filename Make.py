import sys
import os
from xml.dom.minidom import parse as XMLOpen
import subprocess

cwd = os.path.dirname(os.path.abspath(__file__))


def findArg(s):
    if s in sys.argv:
        return sys.argv.index(s)
    return -1

rindex =  findArg("--reference")
if rindex > -1:
    RIMWORLD = sys.argv[rindex+1]
else:
    RIMWORLD = os.environ.get("RWREFERENCE", os.environ['HOME']+'/rwreference')

oindex = findArg("-o")

if oindex > -1:
    OUTPUT = sys.argv[oindex+1]
else:
    OUTPUT=cwd + "/Assemblies/CombatExtended.dll"

cindex = findArg("--csproj")

if cindex > -1:
    CSPROJ = sys.argv[cindex + 1]
else:
    CSPROJ = "Source/CombatExtended/CombatExtended.csproj"

with XMLOpen(CSPROJ) as csproj:
    libraries = [cwd + "/Assemblies/0Harmony.dll"]
    sources = []
    if "--all-libs" in sys.argv:
        for reference in os.listdir(RIMWORLD):
            if reference.endswith(".dll"):
                libraries.append(RIMWORLD+"/"+reference)
        
    else:
        for reference in ['mscorlib.dll']:
            libraries.append(RIMWORLD+'/'+reference)
        
        for reference in csproj.getElementsByTagName("Reference"):
            hintPath = reference.getElementsByTagName("HintPath")
            if hintPath:
                hintPath = hintPath[0].firstChild.data.strip()
                hintPath = hintPath.replace("\\", "/")
                hintPath = hintPath.replace("../../../../RimWorldWin64_Data/Managed", RIMWORLD)
            else:
                hintPath = RIMWORLD+"/"+reference.attributes['Include'].value + '.dll'
            if "0Harmony" in hintPath:
                continue
            if not os.path.exists(hintPath):
                print(f"Library not found: {hintPath}")
                continue
            libraries.append(hintPath)
        
    for source in csproj.getElementsByTagName("Compile"):
        sources.append("Source/CombatExtended/"+source.attributes['Include'].value.replace("\\", "/"))

args = ["mcs", "-nostdlib", "-langversion:Experimental", "-target:library", f'-out:{OUTPUT}', *sources, *[f'-r:{r}' for r in libraries]]

print(libraries)
t = subprocess.Popen(args)



t.wait()
raise SystemExit(t.poll())
