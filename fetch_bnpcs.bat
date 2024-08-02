@echo off
set source="https://raw.githubusercontent.com/ffxiv-teamcraft/ffxiv-teamcraft/staging/libs/data/src/lib/json/gubal-bnpcs-index.json"
set dest="./Ktisis/Data/Library/bnpc-index.json"
curl %source% -o %dest%