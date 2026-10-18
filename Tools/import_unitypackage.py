"""
Extracts a .unitypackage into the project, keeping every asset's GUID (its .meta file).
Used for TMP Essential Resources: AssetDatabase.ImportPackage is deferred in batch mode and never
runs before -quit, so setup cannot import it headless.

    python Tools/import_unitypackage.py <package.unitypackage> [project folder]
"""
import os
import sys
import tarfile


def extract(package, project):
    written = 0
    with tarfile.open(package, "r:gz") as tar:
        entries = {}
        for member in tar.getmembers():
            parts = member.name.replace("\\", "/").strip("./").split("/")
            if len(parts) != 2:
                continue
            guid, kind = parts
            entries.setdefault(guid, {})[kind] = member
        for guid, files in entries.items():
            if "pathname" not in files:
                continue
            path = tar.extractfile(files["pathname"]).read().decode("utf-8").splitlines()[0].strip()
            if not path.startswith("Assets/") or ".." in path:
                print("skipped", path)
                continue
            target = os.path.join(project, *path.split("/"))
            if "asset" in files:
                os.makedirs(os.path.dirname(target), exist_ok=True)
                with open(target, "wb") as out:
                    out.write(tar.extractfile(files["asset"]).read())
                written += 1
            else:
                os.makedirs(target, exist_ok=True)
            if "asset.meta" in files:
                with open(target + ".meta", "wb") as out:
                    out.write(tar.extractfile(files["asset.meta"]).read())
    return written


if __name__ == "__main__":
    project = sys.argv[2] if len(sys.argv) > 2 else os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    print("extracted", extract(sys.argv[1], project), "files")
