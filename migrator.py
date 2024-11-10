from pathlib import Path
from sys import argv
from json import dump, load, JSONDecoder
import re

class JSONCDecoder(JSONDecoder):
	def __init__(self, **kwargs):
		super().__init__(**kwargs)
	
	def decode(self, s):
		s = re.sub(r"""(?=([^"\\]*(\\.|"([^"\\]*\\.)*[^"\\]*"))*[^"]*$)\/\/.*""", "", s)
		# removes any line comment (//) that is not in quotes (has an even number of quotes after it)
		# will break if there are odd quotes in comments but it works for my cases
		s = re.sub(r",(?=\s*[\]}]|$)", "", s)
		# removes trailing commas
		return super().decode(s)

def main(mod_folder: Path):
	print(f"Migrating mod at \"{mod_folder}\"...")
	
	if not mod_folder.is_dir():
		print("Argument should be a mod folder!")
		return

	try:
		with open(mod_folder / "manifest.json") as f:
			manifest = load(f, cls=JSONCDecoder)
	except OSError:
		print("Missing manifest.json!")
		return

	try:
		with open(mod_folder / "content.json") as f:
			content = load(f, cls=JSONCDecoder)
	except OSError:
		print("Missing content.json!")
		return
	
	migrate(mod_folder, manifest, content)

def migrate(mod_folder: Path, manifest: dict, content: dict):
	if ("Format" not in content):
		print("No Format in content")
		return

	if (content["Format"] == 3):
		print("Pack is already in Format 3")
		return
	
	migrate_layers(content)

def migrate_layers(content: dict):
	pass

if __name__ == "__main__":
	if len(argv) == 1:
		print("No mod folder given...")
	
	else:
		for folder in argv[1:]:
			main(Path(folder))