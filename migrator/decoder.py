from json import JSONDecoder
from re import sub

class JSONCDecoder(JSONDecoder):
	def __init__(self, **kwargs):
		super().__init__(**kwargs)
	
	def decode(self, s, *args, **kwargs):
		print("removing line comments")
		lines = s.split('\n')
		print(f"file has {len(lines)} lines")
		for i in range(len(lines)):
			dq_c = 0
			q_c = 0
			for j in range(len(lines[i]) - 1):
				if lines[i][j] == '"':
					dq_c += 1
					continue
				if lines[i][j] == '\'':
					q_c += 1
					continue
				if lines[i][j] == '/' and lines[i][j+1] == '/' and dq_c%2 == 0 and q_c%2 == 0:
					lines[i] = lines[i][:j]
					break

		s = '\n'.join(lines)
		#s = sub(r"""(?=([^"\\]*(\\.|"([^"\\]*\\.)*[^"\\]*"))*[^"]*$)\/\/.*""", "", s) # regex method (very slow)
		# removes any line comment (//) that is not in quotes (has an even number of quotes after it)
		# will break if there are odd quotes in comments but it works for my cases

		print("removing trailing commas")
		s = sub(r",(?=\s*[\]}]|$)", "", s)
		# removes trailing commas
		return super().decode(s, *args, **kwargs)
