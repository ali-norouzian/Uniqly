# Uniqly

A tools for scan files and detect duplicates (from file contents)

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
