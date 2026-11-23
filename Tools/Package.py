"""Build a clean archive with sources, screenshots and the Release app."""

from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile


root = Path(__file__).resolve().parents[1]
archive_path = root / "PolyclinicRegistration_Source_Release.zip"
files = [root / "README.md", root / "PolyclinicRegistration.sln"]

for project_name in ("PolyclinicRegistration", "PolyclinicRegistration.Checks"):
    project = root / project_name
    files.extend(project.rglob("*.cs"))
    files.extend(project.glob("*.csproj"))

release = root / "PolyclinicRegistration" / "bin" / "Release" / "net10.0-windows"
files.extend(path for path in release.rglob("*") if path.is_file() and path.suffix != ".pdb")
files.extend(root / "Screenshots" / name for name in (
    "01-main.png", "03-ticket.png", "04-staff-login.png", "05-staff-journal.png"
))

with ZipFile(archive_path, "w", ZIP_DEFLATED, compresslevel=9) as archive:
    for file in sorted(set(files)):
        if "obj" in file.parts:
            continue
        archive.write(file, file.relative_to(root))

print(archive_path)
print("Files:", len(files))
print("Size MB:", round(archive_path.stat().st_size / 1024 / 1024, 2))
