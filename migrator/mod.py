from pathlib import Path

from manifest import Manifest
from pack import FPack

class Mod:
	def __init__(self, folder: Path):
		self.manifest = Manifest(folder)
		if self.manifest.data is None: return

		self.content = FPack(folder / "content.json", self.manifest)
		if self.content.data is None: return
	
	def migrate(self):
		pass