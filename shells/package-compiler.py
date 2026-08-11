import os
import shutil
import zipfile
from zipfile import ZipFile

def remove_file(file_path) :
    if os.path.exists(file_path) :
        os.remove(file_path)

def remove_dir(dir_path) :
    if os.path.exists(dir_path):
        shutil.rmtree(dir_path)
            

def zip_and_move(base_dir, items, target_dir, version, zip_arch):
    base_dir = base_dir.replace("{ARCH}", zip_arch)
    zip_name = f"-p_compiler_{version}_{zip_arch}.zip"
    zip_path = os.path.join(base_dir, zip_name)

    with ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:
        for item in items:
            full_path = os.path.join(base_dir, item)

            if os.path.isfile(full_path):
                zipf.write(full_path, item)
            else:
                for root, dirs, files in os.walk(full_path):
                    for file in files:
                        full_file = os.path.join(root, file)
                        rel_path = os.path.relpath(full_file, base_dir)
                        zipf.write(full_file, rel_path)

    shutil.move(zip_path, os.path.join(target_dir, zip_name))

rara = input()
Base = os.path.dirname(os.path.abspath(__file__))
Type = "compiler"

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom.exe", "LICENSE", "ThirdPartyNotices.txt", "exclude"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="win-arm64"
)

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom.exe", "LICENSE", "ThirdPartyNotices.txt", "exclude"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="win-x86"
)

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom.exe", "LICENSE", "ThirdPartyNotices.txt", "exclude"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="win-x64"
)

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom", "LICENSE", "ThirdPartyNotices.txt", "exclude"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="linux-x64"
)

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom", "LICENSE", "ThirdPartyNotices.txt"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="linux-arm"
)

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom", "LICENSE", "ThirdPartyNotices.txt"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="linux-arm64"
)

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom", "LICENSE", "ThirdPartyNotices.txt"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="osx-x64"
)

zip_and_move(
    base_dir= Base + "/" + Type + "/publish/{ARCH}",
    items=["mpcom", "LICENSE", "ThirdPartyNotices.txt"],
    target_dir= Base + "/package",
    version=rara,
    zip_arch="osx-arm64"
)
