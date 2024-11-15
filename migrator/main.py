from pathlib import Path
from argparse import ArgumentParser, SUPPRESS

from mod import Mod


def main(mod_folder: Path, output: Path):
	print(f"Migrating mod at \"{mod_folder}\"...")
	
	if not mod_folder.is_dir():
		print("Argument should be a mod folder!")
		return

	mod = Mod(mod_folder)
	mod.migrate()

if __name__ == "__main__":
	parser = ArgumentParser(description="A script to convert FF2 mods to FF3.")
	parser.description += "\nA CP mod will be created at the same time in some cases to preserve features."

	parser.add_argument("input", nargs='*', default=SUPPRESS, help="The path(s) to the folder(s) of the mod(s) to migrate.")
	parser.add_argument("-i", "--input", nargs='*', action="extend", dest="input", help="Same as positional input argument.")
	parser.add_argument("-o", "--output", nargs='*', action="extend", default=[],
		help="The path(s) where the migrated mod(s) will be saved. Will default to creating a folder named \"<input> [FF3]\"."
	)

	args = parser.parse_args()
	print(args)

	inputs = list(map(Path, args.input))
	outputs = list(map(Path, args.output))
	while len(outputs) < len(inputs):
		path = inputs[len(outputs)]
		outputs.append(Path(path.parent) / (path.name + " migrated"))

	for in_folder, out_folder in zip(inputs, outputs):
		main(in_folder, out_folder)