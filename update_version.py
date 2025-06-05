from pathlib import Path
from json import loads, dumps
from argparse import ArgumentParser, SUPPRESS

def update_manifest(path: Path):
	manifest = loads(path.read_text())
	manifest["Version"] = version
	if "ContentPackFor" in manifest:
		if manifest["ContentPackFor"]["UniqueID"] == "leroymilo.FurnitureFramework":
			manifest["ContentPackFor"]["MinimumVersion"] = version
		elif manifest["ContentPackFor"]["UniqueID"] == "Pathoschild.ContentPatcher" and cp_v is not None:
			manifest["ContentPackFor"]["MinimumVersion"] = cp_v
	if api_v is not None:
		manifest["MinimumApiVersion"] = api_v
	if game_v is not None:
		manifest["MinimumGameVersion"] = game_v
	path.write_text(dumps(manifest, indent='\t'))

if __name__ == "__main__":
	parser = ArgumentParser(description="A script to update versions in all manifests.")

	parser.add_argument("version")
	parser.add_argument("-g", "--game", default=None)
	parser.add_argument("-api", default=None)
	parser.add_argument("-cp", default=None)

	args = parser.parse_args()
	version: str = args.version
	game_v: str = args.game
	api_v: str = args.api
	cp_v: str = args.cp
	
	update_manifest(Path("FurnitureFramework/manifest.json"))
	update_manifest(Path("Example Pack/manifest.json"))

	templates = Path("doc/Templates")
	pile = list(templates.iterdir())
	for path in pile:
		if not path.is_dir(): continue
		if (path / "manifest.json").exists():
			update_manifest(path / "manifest.json")
		else:
			pile += list(path.iterdir())