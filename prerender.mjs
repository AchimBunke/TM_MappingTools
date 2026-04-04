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
  args: ['--no-sandbox', '--disable-setuid-sandbox'],
  headless: true
});

const routes = [
  '/',
];

for (const route of routes) {
  console.log(`Prerendering ${route}...`);
  const page = await browser.newPage();

  // Log browser console so we can see what Blazor is doing
  page.on('console', msg => console.log('  [browser]', msg.text()));
  page.on('pageerror', err => console.log('  [page error]', err.message));

  await page.goto(`http://localhost:5500${route}`, {
    waitUntil: 'networkidle0',
    timeout: 60000
  });

  // Wait until the loading spinner is GONE from #app
  // Replace 'nav' with any element your app renders after boot
  try {
    await page.waitForFunction(
      () => {
        const app = document.getElementById('app');
        // Check that loading spinner svg is no longer the only child
        return app && !app.querySelector('svg.loading-progress');
      },
      { timeout: 30000 }
    );
    console.log('  ✅ Blazor finished rendering');
  } catch (e) {
    console.log('  ⚠️ Timed out waiting for Blazor - saving whatever is rendered');
  }

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
