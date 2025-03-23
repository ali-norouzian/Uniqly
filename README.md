# Uniqly

A tools for scan files and detect duplicates (from file contents)

## Find duplicate files in a path

```bash
uniqly --find-duplicates "D:\WithDups" 
```

## Automatic move to recycle bin. (Only keep newest duplicate file)

```bash
uniqly --apply-changes "D:\WithDups" --keep-newest
```
