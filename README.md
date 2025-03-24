# Uniqly

A extremely fast tool for scan files and detect duplicates (from file contents)

## Find duplicate files in a path

Search into path and found duplicate files. Result openned in your default text editor after search completed.

```bash
uniqly search "D:\WithDups" 
```

## Automatic move to recycle bin files that selected as remove in result file

For when, if you forget and closed search result file.

```bash
uniqly apply "D:\WithDups" 
```

## Automatic move to recycle bin. (Only keep newest duplicate file)

```bash
uniqly apply "D:\WithDups" --keep-newest
```

# Sample

## For a folder with 16GB size:

### Search
```bash
uniqly search "D:\Project\"
258504 file checked.
Time taken: 00:02:00.1583558
For appling changes look at: D:\Project\\.UniqlySearchResult
Selected files moved to recycle bin. (Size: 0 B)
```

### Apply
```bash
uniqly apply "D:\Project\" -kn -osi
No file will be deleted. (This is only for showing info)
[00:00:07.3611252] 126404 file deleted.
'10.42 GB' file is ready to delete.
```
