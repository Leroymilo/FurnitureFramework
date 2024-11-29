from copy import deepcopy
from re import sub

from structs import Point, Rectangle
from layers import Layers
from lights import Lights

class FType:
	def __init__(self, data: dict):
		self.data = deepcopy(data)

		self.seasonal = ("Seasonal" in self.data) and self.data["Seasonal"]

		self.source_image = self.data["Source Image"]
		
		self.rots: list[str] = self.data["Rotations"]

		if self.rots == 1:
			self.rots = ["NoRot"]
		elif self.rots == 2:
			self.rots = ["Horizontal", "Vertical"]
		elif self.rots == 4:
			self.rots = ["Down", "Right", "Up", "Left"]

	def migrate(self):

		new_data = deepcopy(self.data)

		for rot_name in self.rots:
			self.migrate(new_data, rot_name)
		
		new_data.pop("Source Rect", None)
		new_data.pop("Light Sources", None)

		# Bed Area change
		if "Bed Area Pixel" in new_data:
			new_data["Bed Area"] = new_data.pop("Bed Area Pixel")
		elif "Bed Area" in new_data:
			new_data["Bed Area"] = (Point(new_data["Bed Area"]) * 16).data

		# FF tokens handling
		new_data["Display Name"] = FType.replace_token(new_data["Display Name"])
		if "Descrition" in new_data:
			new_data["Description"] = FType.replace_token(new_data["Description"])
		if "Shop Id" in new_data:
			new_data["Shop Id"] = FType.replace_token(new_data["Shop Id"])
		if "Shows in Shops" in new_data and type(new_data["Shows in Shops"]) is list:
			for i in range(len(new_data["Shows in Shops"])):
				new_data["Shows in Shops"][i] = FType.replace_token(new_data["Shows in Shops"][i])

		self.data = new_data
	
	@staticmethod
	def replace_token(field: str) -> str:
		return sub("{{(.+?)}}", "[[\\1]]", field)

	def migrate(self, new_data: dict[str], rot: str):
		base_layer_rect = Rectangle(self.data["Source Rect"], rot)

		if "Layers" in self.data:
			layers = Layers(self.data["Layers"], rot, base_layer_rect.height)
		else: layers = Layers([], rot, base_layer_rect.height)

		layers.add_base(base_layer_rect)

		new_data["Layers"][rot] = layers.to_json()

		if "Light Sources" in self.data:
			lights = Lights(self.data["Light Sources"], rot, base_layer_rect.height)
			if "Lights" not in new_data:
				new_data["Lights"] = {}
			new_data["Lights"][rot] = lights.to_json()