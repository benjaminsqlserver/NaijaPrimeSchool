# User manual

`NaijaPrimeSchool-User-Manual.docx` — the illustrated user manual for every role
(SuperAdmin, HeadTeacher, Teacher, SchoolBursar, SchoolStoreKeeper, Parent, Student).
Produced by Benjamin Fadina for HepziBen Technologies Ltd.

To regenerate it after UI changes:

```bash
uat/reset-and-start.sh "<UAT connection string>"   # fresh demo school on :5080
cd docs/manual && npm install
node capture.mjs            # retake the screenshots into shots/
python3 -c "import json,os;from PIL import Image;json.dump({f[:-4]:Image.open('shots/'+f).size for f in os.listdir('shots') if f.endswith('.jpg')},open('shots/sizes.json','w'))"
node build-manual.js        # writes NaijaPrimeSchool-User-Manual.docx
```

Open the document in Word and accept the prompt to update fields so the table of contents fills in.
