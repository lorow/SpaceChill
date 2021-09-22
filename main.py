import argparse
import os
import pprint
import shutil


def main():
    arg_parser = argparse.ArgumentParser(
        description="Sort all substance painter textures to respective folders"
    )
    arg_parser.add_argument(
        "--txt_path", type=str, help="Path to the project's textures folder"
    )

    args = arg_parser.parse_args()
    sort_textures(args.txt_path)


def sort_textures(directory: str) -> None:
    textures = get_textures_list(directory)
    match_texture_with_directories(textures, directory)


def get_directories_list(directory: str) -> list:
    return next(os.walk(directory))[1]


def get_textures_list(directory: str) -> list:
    return next(os.walk(directory))[2]


def match_texture_with_directories(textures: list, directory: str) -> None:
    ignored_textures = []
    for texture in textures:
        # first, claim all the already existing directories
        root_directories = get_directories_list(directory)
        try:
            # then, grab a name for the theme and a name for the item directories
            root_name, object_name = determine_folder_name(texture)
        except TypeError:
            ignored_textures.append(texture)
            continue
        else:
            # if there is a theme directory, check for preexisting items directories and copy the files
            # accordingly
            if root_name not in root_directories:
                os.mkdir(f"{directory}/{root_name}")

            texture_directories = get_directories_list(f"{directory}/{root_name}")
            if object_name not in texture_directories:
                os.mkdir(f"{directory}/{root_name}/{object_name}")

            shutil.move(
                f"{directory}/{texture}",
                f"{directory}/{root_name}/{object_name}/{texture}",
            )

    print("Skipped files:")
    pprint.pprint(ignored_textures)


def determine_folder_name(texture: str) -> tuple or None:
    split_texture_name = texture.split("_")
    # 0 - root name, 1 - file name thanks to $mesh_$textureSet_* being the default export pattern
    try:
        return (
            split_texture_name[0],
            split_texture_name[1],
        )
    except IndexError:
        return None


if __name__ == "__main__":
    main()
