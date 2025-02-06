from copy import deepcopy
from re import sub

from structs import Point, Rectangle
from properties.layers import Layers
from properties.lights import Lights
from properties.particles import Particles
from properties.slots import Slots

class FType:
	def __init__(self, data: dict):
		self.data = deepcopy(data)

		self.seasonal = ("Seasonal" in self.data) and self.data["Seasonal"]
		self.data.pop("Seasonal", None)

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
		
		new_data.pop("Source Rect", None)
		new_data.pop("Light Sources", None)

		new_data["Layers"] = {}
		new_data["Lights"] = {}
		new_data["Particles"] = {}
		new_data["Slots"] = {}
		for rot_name in self.rots:
			self.migrate_rot(new_data, rot_name)
		
		# Animation
		anim = {}
		for key in ["Frame Count", "Frame Duration", "Animation Offset"]:
			if key in self.data:
				anim[key] = self.data[key]
				new_data.pop(key)
		if len(anim) > 0:
			new_data["Animation"] = anim

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

	def migrate_rot(self, new_data: dict[str], rot: str):
		base_layer_rect = Rectangle(self.data["Source Rect"], rot)

		# Fish Area

		if "Fish Area" in self.data:
			area = deepcopy(self.data["Fish Area"])
			area["Y"] -= base_layer_rect.height
			if "Fish Area" not in new_data:
				new_data["Fish Area"] = {}
			new_data["Fish Area"][rot] = area

		# Screen Position

		if "Screen Position" in self.data:
			area = deepcopy(self.data["Screen Position"])
			area["Y"] -= base_layer_rect.height
			if "Screen Position" not in new_data:
				new_data["Screen Position"] = {}
			new_data["Screen Position"][rot] = area

		# Layers

		if "Layers" in self.data:
			layers = Layers(self.data["Layers"], rot, base_layer_rect.height)
		else: layers = Layers([], rot, base_layer_rect.height)

		new_data["Layers"][rot] = layers.to_json()
		new_data["Layers"][rot].insert(0, {"Source Rect": base_layer_rect.data, "Depth": 0})

		# Lights

		if "Light Sources" in self.data:
			lights = Lights(self.data["Light Sources"], rot, base_layer_rect.height)
			new_data["Lights"][rot] = lights.to_json()
	
		# Particles (spawn area moved)

		if "Particles" in self.data:
			particles = Particles(self.data["Particles"], rot, base_layer_rect.height)
			new_data["Particles"][rot] = particles.to_json()

		# Slots (area)

		if "Slots" in self.data:
			slots = Slots(self.data["Slots"], rot, base_layer_rect.height)
			new_data["Slots"][rot] = slots.to_json()