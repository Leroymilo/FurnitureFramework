from json import JSONDecoder
from re import sub

class JSONCDecoder(JSONDecoder):
	def __init__(self, **kwargs):
		super().__init__(**kwargs)
	
	def decode(self, s):
		s = sub(r"""(?=([^"\\]*(\\.|"([^"\\]*\\.)*[^"\\]*"))*[^"]*$)\/\/.*""", "", s)
		# removes any line comment (//) that is not in quotes (has an even number of quotes after it)
		# will break if there are odd quotes in comments but it works for my cases
		s = sub(r",(?=\s*[\]}]|$)", "", s)
		# removes trailing commas
		return super().decode(s)