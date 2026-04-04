import puppeteer from 'puppeteer';
import { spawn } from 'child_process';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

const server = spawn('npx', ['serve', '.', '-p', '5500', '--no-clipboard'], {
  stdio: 'ignore',
  detached: true
});

await new Promise(r => setTimeout(r, 3000));

const browser = await puppeteer.launch({
  args: ['--no-sandbox', '--disable-setuid-sandbox']
});

const routes = [
  '/',
];

for (const route of routes) {
  console.log(`Prerendering ${route}...`);
  const page = await browser.newPage();

  await page.goto(`http://localhost:5500${route}`, {
    waitUntil: 'networkidle0',
    timeout: 60000
  });

  await new Promise(r => setTimeout(r, 5000));

  const html = await page.content();
  await page.close();

  const outDir = join('.', route === '/' ? '' : route);
  mkdirSync(outDir, { recursive: true });
  writeFileSync(join(outDir, 'index.html'), html);
  console.log(`  ✅ Done: ${route}`);
}

await browser.close();
server.kill();
console.log('Prerendering complete.');
