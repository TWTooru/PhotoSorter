### Task 1: Install MetadataExtractor Package

**Files:**
- Modify: `photo/photo/photo.csproj`

**Interfaces:**
- Consumes: `photo.sln`
- Produces: `photo.csproj` with NuGet dependency

- [ ] **Step 1: Install MetadataExtractor**

Run in `photo/photo` directory:
```bash
dotnet add package MetadataExtractor
```

- [ ] **Step 2: Verify Installation**

Run: `dotnet restore photo/photo.sln`
Expected: Succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add photo/photo/photo.csproj
git commit -m "build: add MetadataExtractor package"
```
