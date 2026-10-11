package com.example.messengersmartarchive;

import android.app.Activity;
import android.app.AlertDialog;
import android.os.Bundle;
import android.content.Intent;
import android.net.Uri;
import android.view.Gravity;
import android.widget.*;
import android.graphics.Color;
import org.json.*;
import java.io.*;
import java.util.*;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;
import java.util.regex.*;
import java.nio.charset.StandardCharsets;

public class MainActivity extends Activity {
    static class Item {
        String kind, value, sender, thread, time, source, excerpt;
        Item(String k,String v,String s,String th,String ti,String so,String ex) {
            kind=k; value=v; sender=s; thread=th; time=ti; source=so; excerpt=ex;
        }
    }
    static class ImportResult {
        int files = 0;
        int messages = 0;
    }

    final ArrayList<Item> all = new ArrayList<>();
    LinearLayout results;
    EditText searchBox;
    Spinner typeSpinner;
    TextView status, details;
    Button importBtn, searchBtn;
    final String[] types={"الكل","الرسائل","رقم هاتف","بريد إلكتروني","عنوان محتمل"};
    final Pattern phone=Pattern.compile("(?<!\\w)(?:\\+?\\d[\\d\\s().\\-]{6,}\\d)(?!\\w)");
    final Pattern email=Pattern.compile("\\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}\\b");
    final Pattern address=Pattern.compile("(عنوان|العنوان|شارع|زقاق|محلة|منطقة|حيّ|حي |بناية|دار رقم|address|street|road|avenue|district|building)",Pattern.CASE_INSENSITIVE);

    @Override public void onCreate(Bundle b) {
        super.onCreate(b);
        getWindow().setStatusBarColor(Color.rgb(24,49,83));
        LinearLayout page=new LinearLayout(this); page.setOrientation(LinearLayout.VERTICAL);
        page.setPadding(20,18,20,12); page.setBackgroundColor(Color.rgb(244,246,249));
        TextView title=new TextView(this); title.setText("أرشيف ماسنجر الذكي"); title.setTextSize(24); title.setTextColor(Color.WHITE); title.setGravity(Gravity.RIGHT); title.setPadding(16,18,16,18); title.setBackgroundColor(Color.rgb(24,49,83)); page.addView(title);
        TextView intro=new TextView(this); intro.setText("بحث في محادثاتك • استخراج الأرقام والبريد والعناوين المحتملة"); intro.setTextSize(14); intro.setGravity(Gravity.RIGHT); intro.setPadding(0,10,0,4); page.addView(intro);
        TextView notice=new TextView(this); notice.setText("مهم: التطبيق لا يدخل إلى حساب فيسبوك مباشرة. نزّل نسخة رسائلك من فيسبوك ثم استورد ملف ZIP أو JSON هنا."); notice.setTextSize(13); notice.setGravity(Gravity.RIGHT); notice.setTextColor(Color.rgb(110,55,25)); notice.setPadding(10,8,10,8); notice.setBackgroundColor(Color.rgb(255,241,224)); page.addView(notice);
        importBtn=new Button(this); importBtn.setText("استيراد نسخة المحادثات (ZIP أو JSON)"); importBtn.setOnClickListener(v->pickFile()); page.addView(importBtn);
        Button helpBtn=new Button(this); helpBtn.setText("كيف أحصل على نسخة رسائلي؟"); helpBtn.setOnClickListener(v->showExportHelp()); page.addView(helpBtn);
        searchBox=new EditText(this); searchBox.setSingleLine(true); searchBox.setHint("اكتب اسمًا أو رقمًا أو كلمة للبحث..."); searchBox.setGravity(Gravity.RIGHT); page.addView(searchBox);
        typeSpinner=new Spinner(this); typeSpinner.setAdapter(new ArrayAdapter<>(this,android.R.layout.simple_spinner_dropdown_item,types)); page.addView(typeSpinner);
        searchBtn=new Button(this); searchBtn.setText("بحث"); searchBtn.setOnClickListener(v->search()); page.addView(searchBtn);
        status=new TextView(this); status.setText("لم تُستورد محادثات بعد"); status.setGravity(Gravity.RIGHT); status.setPadding(0,8,0,8); page.addView(status);
        ScrollView scroll=new ScrollView(this); results=new LinearLayout(this); results.setOrientation(LinearLayout.VERTICAL); scroll.addView(results);
        page.addView(scroll,new LinearLayout.LayoutParams(-1,0,1));
        details=new TextView(this); details.setText("بعد الاستيراد، اضغط على أي نتيجة لعرض التفاصيل."); details.setTextSize(14); details.setTextColor(Color.DKGRAY); details.setPadding(10,12,10,12); details.setBackgroundColor(Color.WHITE); page.addView(details);
        setContentView(page);
    }

    void pickFile() {
        Intent i=new Intent(Intent.ACTION_OPEN_DOCUMENT);
        i.setType("*/*");
        i.putExtra(Intent.EXTRA_MIME_TYPES,new String[]{"application/zip","application/x-zip-compressed","application/json","application/octet-stream"});
        i.addCategory(Intent.CATEGORY_OPENABLE);
        startActivityForResult(i,10);
    }

    void showExportHelp() {
        String msg="لإحضار رسائلك إلى هذا التطبيق:\n\n"+
                "1) افتح مركز حسابات فيسبوك من الزر أدناه.\n"+
                "2) ادخل إلى معلوماتك وأذوناتك ثم تنزيل معلوماتك.\n"+
                "3) اختر حساب فيسبوك، ثم بعض معلوماتك، وحدد الرسائل فقط.\n"+
                "4) اختر كل الفترة الزمنية، والصيغة JSON، ثم أنشئ ملف التنزيل.\n"+
                "5) عندما يصبح الملف جاهزًا نزّله على الهاتف، ثم ارجع إلى التطبيق واختر ملف ZIP كما هو؛ لا تحتاج إلى فك الضغط.\n\n"+
                "قد تختلف أسماء القوائم قليلًا حسب إصدار فيسبوك. لا تشارك ملف التصدير مع أي شخص لأنه يحتوي على رسائلك الخاصة.";
        new AlertDialog.Builder(this).setTitle("طريقة الحصول على الرسائل").setMessage(msg)
                .setPositiveButton("فتح مركز حسابات فيسبوك",(d,w)->{
                    try { startActivity(new Intent(Intent.ACTION_VIEW,Uri.parse("https://accountscenter.facebook.com/info_and_permissions/"))); }
                    catch(Exception e) { startActivity(new Intent(Intent.ACTION_VIEW,Uri.parse("https://www.facebook.com/"))); }
                }).setNegativeButton("إغلاق",null).show();
    }

    @Override protected void onActivityResult(int req,int res,Intent data) {
        super.onActivityResult(req,res,data);
        if(req==10 && res==RESULT_OK && data!=null && data.getData()!=null) {
            Uri uri=data.getData();
            try { getContentResolver().takePersistableUriPermission(uri,Intent.FLAG_GRANT_READ_URI_PERMISSION); } catch(Exception ignored){}
            importBtn.setEnabled(false); searchBtn.setEnabled(false);
            status.setText("جارٍ استيراد الملف... قد يستغرق ذلك وقتًا إذا كانت المحادثات كثيرة.");
            new Thread(()->{
                try {
                    ImportResult result;
                    if(isZip(uri)) result=importZip(uri);
                    else result=importSingleJson(uri);
                    final ImportResult done=result;
                    runOnUiThread(()->{
                        importBtn.setEnabled(true); searchBtn.setEnabled(true);
                        if(done.files==0) {
                            status.setText("لم يتم العثور على ملفات محادثات صالحة.");
                            new AlertDialog.Builder(this).setTitle("لم أجد محادثات في الملف")
                                .setMessage("اختر ملف ZIP الذي نزّلته من فيسبوك بصيغة JSON، أو ملف message_1.json من داخل مجلد الرسائل. ملف HTML أو الصور ليس مناسبًا لهذا التطبيق.")
                                .setPositiveButton("حسنًا",null).show();
                        } else {
                            status.setText("تم الاستيراد: "+done.messages+" رسالة من "+done.files+" ملف محادثة. إجمالي عناصر البحث: "+all.size());
                            search();
                            new AlertDialog.Builder(this).setTitle("اكتمل الاستيراد")
                                .setMessage("ملفات المحادثات: "+done.files+"\nالرسائل المقروءة: "+done.messages+"\nالعناصر المتاحة للبحث: "+all.size()+"\n\nتستطيع الآن البحث بالاسم أو رقم الهاتف أو أي كلمة وردت في الرسائل.")
                                .setPositiveButton("حسنًا",null).show();
                        }
                    });
                } catch(Exception e) {
                    runOnUiThread(()->{
                        importBtn.setEnabled(true); searchBtn.setEnabled(true);
                        status.setText("تعذّر الاستيراد. جرّب ملف ZIP أو JSON من فيسبوك.");
                        new AlertDialog.Builder(this).setTitle("تعذّر الاستيراد")
                            .setMessage((e.getMessage()==null?"تعذر قراءة الملف المحدد.":e.getMessage())+"\n\nاضغط «كيف أحصل على نسخة رسائلي؟» لمعرفة الطريقة.")
                            .setPositiveButton("حسنًا",null).show();
                    });
                }
            }).start();
        }
    }

    boolean isZip(Uri uri) throws Exception {
        InputStream in=getContentResolver().openInputStream(uri);
        if(in==null) throw new Exception("تعذر فتح الملف المحدد.");
        int a=in.read(), b=in.read(); in.close();
        return a==0x50 && b==0x4b;
    }

    ImportResult importSingleJson(Uri uri) throws Exception {
        String raw=readUri(uri);
        JSONObject data=new JSONObject(raw);
        int n=addJsonConversation(data,uri.toString());
        if(n<0) throw new Exception("الملف لا يحتوي على قائمة الرسائل المطلوبة. اختر ZIP من تنزيل فيسبوك أو ملف message_1.json.");
        ImportResult r=new ImportResult(); r.files=1; r.messages=n; return r;
    }

    ImportResult importZip(Uri uri) throws Exception {
        ImportResult result=new ImportResult();
        InputStream raw=getContentResolver().openInputStream(uri);
        if(raw==null) throw new Exception("تعذر فتح ملف ZIP.");
        ZipInputStream zin=new ZipInputStream(new BufferedInputStream(raw));
        ZipEntry entry;
        while((entry=zin.getNextEntry())!=null) {
            if(!entry.isDirectory() && entry.getName().toLowerCase(Locale.ROOT).endsWith(".json")) {
                String text=readEntry(zin);
                try {
                    JSONObject data=new JSONObject(text);
                    int n=addJsonConversation(data,uri.toString()+" ["+entry.getName()+"]");
                    if(n>=0) { result.files++; result.messages+=n; }
                } catch(JSONException ignored) {
                    // Other JSON files in a Facebook export are not conversation files.
                }
            }
            zin.closeEntry();
        }
        zin.close();
        return result;
    }

    String readEntry(InputStream in) throws IOException {
        ByteArrayOutputStream out=new ByteArrayOutputStream();
        byte[] buf=new byte[8192]; int n;
        while((n=in.read(buf))!=-1) out.write(buf,0,n);
        return new String(out.toByteArray(),StandardCharsets.UTF_8);
    }

    String readUri(Uri uri) throws Exception {
        InputStream in=getContentResolver().openInputStream(uri);
        if(in==null) throw new Exception("تعذر فتح الملف المحدد.");
        try { return readEntry(in); } finally { in.close(); }
    }

    int addJsonConversation(JSONObject data,String source) {
        JSONArray messages=data.optJSONArray("messages");
        if(messages==null) return -1;
        String thread=data.optString("title","محادثة");
        JSONArray ps=data.optJSONArray("participants");
        if(ps!=null) {
            ArrayList<String> names=new ArrayList<>();
            for(int i=0;i<ps.length();i++) { JSONObject p=ps.optJSONObject(i); if(p!=null&&!p.optString("name","").isEmpty()) names.add(p.optString("name")); }
            if(!names.isEmpty()) thread=String.join("، ",names);
        }
        int added=0;
        for(int i=0;i<messages.length();i++) {
            JSONObject m=messages.optJSONObject(i); if(m==null) continue;
            String sender=m.optString("sender_name",""), time=m.optString("timestamp_ms","");
            if(!time.isEmpty()) try { time=new java.text.SimpleDateFormat("yyyy-MM-dd HH:mm",Locale.getDefault()).format(new Date(Long.parseLong(time))); } catch(Exception ignored){}
            StringBuilder body=new StringBuilder(); JSONArray content=m.optJSONArray("content");
            if(content!=null) for(int j=0;j<content.length();j++) { JSONObject c=content.optJSONObject(j); if(c!=null) { String t=c.optString("text",""); if(!t.isEmpty()) {if(body.length()>0) body.append('\n'); body.append(t);} } }
            if(body.length()==0) body.append(m.optString("text",""));
            if(body.toString().trim().isEmpty()) continue;
            String text=body.toString();
            all.add(new Item("رسالة",text,sender,thread,time,source,text)); added++;
            Matcher pm=phone.matcher(text);
            while(pm.find()) { String v=pm.group().trim(); String digits=v.replaceAll("\\D",""); if(digits.length()>=7 && digits.length()<=15) all.add(new Item("رقم هاتف",v,sender,thread,time,source,snippet(text,pm.start(),pm.end()))); }
            Matcher em=email.matcher(text);
            while(em.find()) all.add(new Item("بريد إلكتروني",em.group(),sender,thread,time,source,snippet(text,em.start(),em.end())));
            for(String line:text.split("[\\n\\r؛;.!؟]+")) if(address.matcher(line).find() && line.trim().length()>=6 && line.trim().length()<=240) all.add(new Item("عنوان محتمل",line.trim(),sender,thread,time,source,line.trim()));
        }
        return added;
    }

    String snippet(String s,int a,int b) { return s.substring(Math.max(0,a-80),Math.min(s.length(),b+80)); }

    void search() {
        results.removeAllViews(); String q=searchBox.getText().toString().trim().toLowerCase(Locale.ROOT);
        String kind=String.valueOf(typeSpinner.getSelectedItem()); int count=0;
        for(Item x:all) {
            if(!kind.equals("الكل") && !kind.equals(x.kind)) continue;
            String hay=(x.value+" "+x.sender+" "+x.thread+" "+x.excerpt).toLowerCase(Locale.ROOT);
            if(!q.isEmpty() && !hay.contains(q)) continue;
            if(count++>=500) break;
            TextView row=new TextView(this); row.setText("["+x.kind+"] "+x.value.replace('\n',' ')+(x.value.length()>170?"…":"")+"\n"+x.sender+" • "+x.thread+" • "+x.time);
            row.setTextSize(14); row.setGravity(Gravity.RIGHT); row.setPadding(12,12,12,12); row.setBackgroundColor(count%2==0?Color.WHITE:Color.rgb(235,240,246));
            LinearLayout.LayoutParams lp=new LinearLayout.LayoutParams(-1,-2); lp.setMargins(0,2,0,2); results.addView(row,lp);
            row.setOnClickListener(v->details.setText("النوع: "+x.kind+"\nالمعلومة: "+x.value+"\nالمرسل: "+x.sender+"\nالمحادثة: "+x.thread+"\nالتاريخ: "+x.time+"\n\nالنص المحيط: "+x.excerpt+"\n\nالمصدر: "+x.source));
        }
        status.setText("إجمالي عناصر البحث في الجلسة: "+all.size()+" • النتائج المعروضة: "+count);
    }
}
