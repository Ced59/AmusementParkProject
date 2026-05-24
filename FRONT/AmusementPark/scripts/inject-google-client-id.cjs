const fs = require('fs');
const path = require('path');

const googleClientId = process.env.GOOGLE_CLIENT_ID;
const filePath = path.join('src', 'environments', 'environment.prod.ts');

if (!googleClientId || googleClientId.trim().length === 0) {
  console.log('GOOGLE_CLIENT_ID build arg is empty; using environment file value.');
  process.exit(0);
}

const sanitizedGoogleClientId = googleClientId.trim();
const escapedGoogleClientId = sanitizedGoogleClientId
  .replace(/\\/g, '\\\\')
  .replace(/'/g, "\\'");

if (!fs.existsSync(filePath)) {
  throw new Error(`Environment file not found: ${filePath}.`);
}

const currentContent = fs.readFileSync(filePath, 'utf8');
let updatedContent = currentContent;

const googleClientIdPropertyPattern = /(googleClientId\s*:\s*)(['"])([^'"]*)(['"])/m;

if (googleClientIdPropertyPattern.test(updatedContent)) {
  updatedContent = updatedContent.replace(
    googleClientIdPropertyPattern,
    `$1'${escapedGoogleClientId}'`
  );
} else {
  const apiImagePathPattern = /(apiImagePath\s*:\s*['"][^'"]*['"]\s*,\s*)/m;
  const productionPattern = /(production\s*:\s*true\s*,\s*)/m;

  if (apiImagePathPattern.test(updatedContent)) {
    updatedContent = updatedContent.replace(
      apiImagePathPattern,
      `$1  googleClientId: '${escapedGoogleClientId}',\n  `
    );
  } else if (productionPattern.test(updatedContent)) {
    updatedContent = updatedContent.replace(
      productionPattern,
      `$1  googleClientId: '${escapedGoogleClientId}',\n  `
    );
  } else {
    throw new Error(`Could not inject googleClientId into ${filePath}: no known insertion point found.`);
  }
}

if (currentContent === updatedContent) {
  console.log(`Google client id already up to date in ${filePath}.`);
  process.exit(0);
}

fs.writeFileSync(filePath, updatedContent);
console.log(`Injected Google client id into ${filePath}.`);
