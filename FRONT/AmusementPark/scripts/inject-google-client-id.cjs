const fs = require('fs');

const googleClientId = process.env.GOOGLE_CLIENT_ID;
const filePath = 'src/environments/environment.prod.ts';

if (!googleClientId || googleClientId.trim().length === 0) {
  console.log('GOOGLE_CLIENT_ID build arg is empty; using environment file value.');
  process.exit(0);
}

const escapedGoogleClientId = googleClientId.trim().replace(/\\/g, '\\\\').replace(/'/g, "\\'");
const currentContent = fs.readFileSync(filePath, 'utf8');
const updatedContent = currentContent.replace(
  /googleClientId: '[^']*'/,
  `googleClientId: '${escapedGoogleClientId}'`
);

if (currentContent === updatedContent) {
  throw new Error(`Could not find googleClientId in ${filePath}.`);
}

fs.writeFileSync(filePath, updatedContent);
console.log(`Injected Google client id into ${filePath}.`);
